using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace EtherGizmos.Common.Services;

internal class MessageReceiver : IMessageReceiver
{
    private readonly MessagingOptions _options;

    public IServiceProvider Services { get; }

    public MessageReceiver(
        IServiceProvider services,
        IOptions<MessagingOptions> options)
    {
        _options = options.Value;
        Services = services;
    }

    public async Task ReceiveAsync(
        ReceivedMessage message,
        CancellationToken cancellationToken = default)
    {
        var type = _options.ConvertType(message.Type);

        var method = typeof(MessageReceiver)
            .GetMethod(nameof(ReceiveInternalAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(type);

        var result = (Task)method.Invoke(this, [message, cancellationToken])!;
        await result.ConfigureAwait(false);

        if (!message.Actions.Invoked)
            await message.Actions.CompleteAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task ReceiveInternalAsync<TMessage>(
        ReceivedMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class, new()
    {
        var logicalName = message.LogicalSourceName;

        using var scope = Services.CreateScope();
        var provider = scope.ServiceProvider;

        //Fetch the global and local transformers
        var globalTransformers = provider.GetRequiredService<IEnumerable<IMessageTransformer>>();
        var localTransformers = provider.GetRequiredKeyedService<IEnumerable<IMessageTransformer>>(logicalName);

        var transformers = globalTransformers.Concat(localTransformers).Reverse();

        //Prefer an endpoint-specific serializer, but fall back to the global one
        var serializer = provider.GetService<IMessageSerializer>()
            ?? provider.GetRequiredKeyedService<IMessageSerializer>(logicalName);

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
            var localMiddleware = provider.GetRequiredKeyedService<IEnumerable<IMessageMiddleware>>(logicalName);

            var middleware = globalMiddleware.Concat(localMiddleware);

            try
            {
                //Transform the message back into its original state
                foreach (var transformer in transformers)
                {
                    useMessage = await transformer.UnwrapAsync(useMessage, ct);
                }

                //Deserialize the message payload
                var deserialized = serializer.Deserialize<TMessage>(message.Body);
                var context = new MessageContext<TMessage>(deserialized, message.Actions, cancellationToken);

                //The final pipeline step is to execute the consumer
                var execute = async () =>
                {
                    await Parallel.ForEachAsync(consumers, async (consumer, ct) =>
                    {
                        await consumer.ConsumeAsync(context).ConfigureAwait(false);
                    }).ConfigureAwait(false);
                };

                //Build the pipeline from the middleware; we wrap starting from the innermost
                var pipeline = globalMiddleware
                    .Reverse()
                    .Aggregate(execute, (acc, m) =>
                        () => m.InvokeAsync(message, acc));

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
}
