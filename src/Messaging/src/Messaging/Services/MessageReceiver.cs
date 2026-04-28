using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace EtherGizmos.Common.Services;

internal class MessageReceiver : IMessageReceiver
{
    private readonly IMessageBusRegistry _registry;
    private readonly IOptionsMonitor<MessagingOptions> _options;

    public IServiceProvider Services { get; }

    public MessageReceiver(
        IServiceProvider services,
        IMessageBusRegistry registry,
        IOptionsMonitor<MessagingOptions> options)
    {
        _options = options;
        _registry = registry;
        Services = services;
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
            .GetMethod(nameof(ReceiveInternalAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(type);

        var result = (Task)method.Invoke(this, [busId, message, cancellationToken])!;
        await result.ConfigureAwait(false);

        if (!message.Actions.Invoked)
            await message.Actions.CompleteAsync(cancellationToken).ConfigureAwait(false);
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

        //Fetch the global and local transformers
        var globalTransformers = provider.GetRequiredService<IEnumerable<IMessageTransformer>>();
        var busTransformers = provider.GetRequiredKeyedService<IEnumerable<IMessageTransformer>>(busKey);
        var localTransformers = provider.GetRequiredKeyedService<IEnumerable<IMessageTransformer>>(logicalKey);

        var transformers = globalTransformers.Concat(busTransformers).Concat(localTransformers).Reverse();

        //Prefer an endpoint-specific serializer, but fall back to the global one
        var serializer = provider.GetKeyedService<IMessageSerializer>(logicalKey)
            ?? provider.GetKeyedService<IMessageSerializer>(busKey)
            ?? provider.GetRequiredService<IMessageSerializer>();

        //Fetch all consumers of the message
        var consumers = provider.GetRequiredService<IEnumerable<IMessageConsumer<TMessage>>>();
        if (!consumers.Any())
            throw new InvalidOperationException($"No consumers available for type {typeof(TMessage)}");

        //Keep track of any failed consumers
        var exceptions = new ConcurrentBag<Exception>();

        //We need to handle duplicate consumers in parallel
        await Parallel.ForEachAsync(consumers, async (consumer, ct) =>
        {
            var useMessage = message with { ConsumerName = consumer.GetType().FullName };

            //Fetch the middlware
            var globalMiddleware = provider.GetRequiredService<IEnumerable<IMessageMiddleware>>();
            var busMiddleware = provider.GetRequiredKeyedService<IEnumerable<IMessageMiddleware>>(busKey);
            var localMiddleware = provider.GetRequiredKeyedService<IEnumerable<IMessageMiddleware>>(logicalKey);

            var middleware = globalMiddleware.Concat(busMiddleware).Concat(localMiddleware);

            try
            {
                //Transform the message back into its original state
                foreach (var transformer in transformers)
                {
                    useMessage = await transformer.UnwrapAsync(useMessage, ct);
                }

                //Deserialize the message payload
                var deserialized = serializer.Deserialize<TMessage>(useMessage.Body);
                var context = new MessageContext<TMessage>(deserialized, useMessage.Actions, cancellationToken);

                //The final pipeline step is to execute the consumer
                async Task Execute()
                {
                    using var activity = ActivitySources.Messaging.StartActivityFromCarrier(
                        $"Process {useMessage.Type} via {useMessage.ConsumerName}",
                        ActivityKind.Consumer,
                        useMessage.Headers);

                    activity?.SetTag("messaging.operation.name", "process");
                    activity?.SetTag("messaging.source.name", useMessage.LogicalSourceName);
                    activity?.SetTag("messaging.message.type", useMessage.Type);
                    activity?.SetTag("messaging.consumer.name", useMessage.ConsumerName);

                    await consumer.ConsumeAsync(context).ConfigureAwait(false);
                }

                //Build the pipeline from the middleware; we wrap starting from the innermost
                var pipeline = middleware
                    .Reverse()
                    .Aggregate(Execute, (acc, m) =>
                        () => m.InvokeAsync(useMessage, acc));

                await pipeline().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        //Throw them once all consumers are done
        if (exceptions.Any())
            throw new AggregateException(exceptions);
    }

    [DoesNotReturn]
    private void ThrowForListener(
        string logicalName)
        => throw new InvalidOperationException($"No listener registered for {logicalName}");
}
