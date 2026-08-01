using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace EtherGizmos.Common.Services;

internal sealed class MessageReceiver : IMessageReceiver
{
    private readonly IMessageBusRegistry _registry;
    private readonly IOptionsMonitor<MessagingOptions> _options;

    public IServiceProvider Services { get; }

    public MessageReceiver(
        IServiceProvider services,
        IMessageBusRegistry registry,
        IOptionsMonitor<MessagingOptions> options)
    {
        Services = services;
        _registry = registry;
        _options = options;
    }

    public async Task ReceiveAsync(
        ReceivedMessage message,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Messaging.StartActivityFromCarrier(
            $"Receive {message.Type} from {message.LogicalSourceName}",
            ActivityKind.Consumer,
            message.Headers);

        activity?.SetTag("messaging.operation.name", "receive");
        activity?.SetTag("messaging.source.name", message.LogicalSourceName);
        activity?.SetTag("messaging.message.type", message.Type);

        message = message.AddActivityHeaders(activity);
        var logicalName = message.LogicalSourceName;

        if (!_registry.TryGetBusId(logicalName, out var busId))
            ThrowForListener(logicalName);

        var type = _options.Get(busId).ConvertType(message.Type);
        var method = typeof(MessageReceiver)
            .GetMethod(
                nameof(ReceiveInternalAsync),
                BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(type);

        try
        {
            var result = (Task)method.Invoke(
                this,
                [busId, message, cancellationToken])!;

            await result.ConfigureAwait(false);
        }
        catch (TargetInvocationException ex)
            when (ex.InnerException is not null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }

        if (!message.Actions.Invoked)
        {
            await message.Actions
                .CompleteAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task ReceiveInternalAsync<TMessage>(
        string busId,
        ReceivedMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class, new()
    {
        var logicalName = message.LogicalSourceName;
        var busKey = new BusKey(busId);
        var logicalKey = new BusKey(busId, logicalName);

        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider;

        var globalTransformers = provider.GetRequiredService<IEnumerable<IMessageTransformer>>();
        var busTransformers = provider.GetRequiredKeyedService<IEnumerable<IMessageTransformer>>(busKey);
        var localTransformers = provider.GetRequiredKeyedService<IEnumerable<IMessageTransformer>>(logicalKey);
        var transformers = globalTransformers
            .Concat(busTransformers)
            .Concat(localTransformers)
            .Reverse()
            .ToArray();

        var serializer = provider.GetKeyedService<IMessageSerializer>(logicalKey)
            ?? provider.GetKeyedService<IMessageSerializer>(busKey)
            ?? provider.GetRequiredService<IMessageSerializer>();

        var consumers = provider
            .GetRequiredService<IEnumerable<IMessageConsumer<TMessage>>>()
            .ToArray();

        if (consumers.Length == 0)
        {
            throw new InvalidOperationException(
                $"No consumers are available for type '{typeof(TMessage)}'.");
        }

        var globalMiddleware = provider.GetRequiredService<IEnumerable<IMessageMiddleware>>();
        var busMiddleware = provider.GetRequiredKeyedService<IEnumerable<IMessageMiddleware>>(busKey);
        var localMiddleware = provider.GetRequiredKeyedService<IEnumerable<IMessageMiddleware>>(logicalKey);
        var middleware = globalMiddleware
            .Concat(busMiddleware)
            .Concat(localMiddleware)
            .ToArray();

        var exceptions = new ConcurrentBag<Exception>();
        var parallelOptions = new ParallelOptions
        {
            CancellationToken = cancellationToken,
        };

        await Parallel.ForEachAsync(
            consumers,
            parallelOptions,
            async (consumer, ct) =>
            {
                var useMessage = message with
                {
                    ConsumerName = consumer.GetType().FullName,
                };

                try
                {
                    foreach (var transformer in transformers)
                    {
                        useMessage = await transformer
                            .UnwrapAsync(useMessage, ct)
                            .ConfigureAwait(false);
                    }

                    var deserialized = serializer.Deserialize<TMessage>(useMessage.Body);
                    var context = new MessageContext<TMessage>(
                        deserialized,
                        useMessage,
                        useMessage.Actions,
                        ct);

                    async Task ExecuteAsync()
                    {
                        using var activity = ActivitySources.Messaging.StartActivityFromCarrier(
                            $"Process {useMessage.Type} via {useMessage.ConsumerName}",
                            ActivityKind.Consumer,
                            useMessage.Headers);

                        activity?.SetTag("messaging.operation.name", "process");
                        activity?.SetTag("messaging.source.name", useMessage.LogicalSourceName);
                        activity?.SetTag("messaging.message.type", useMessage.Type);
                        activity?.SetTag("messaging.consumer.name", useMessage.ConsumerName);

                        await consumer
                            .ConsumeAsync(context)
                            .ConfigureAwait(false);
                    }

                    var pipeline = middleware
                        .AsEnumerable()
                        .Reverse()
                        .Aggregate(
                            ExecuteAsync,
                            (next, component) =>
                                () => component.InvokeAsync(useMessage, next));

                    await pipeline().ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            }).ConfigureAwait(false);

        if (!exceptions.IsEmpty)
            throw new AggregateException(exceptions);
    }

    [DoesNotReturn]
    private static void ThrowForListener(string logicalName)
        => throw new InvalidOperationException(
            $"No listener is registered for '{logicalName}'.");
}
