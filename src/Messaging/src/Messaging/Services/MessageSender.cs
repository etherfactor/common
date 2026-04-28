using EtherGizmos.Common.Abstractions;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common.Services;

internal class MessageSender : IMessageSender, ITransportMessageSender
{
    private readonly IMessageBusRegistry _registry;

    public IServiceProvider Services { get; }

    public MessageSender(
        IServiceProvider services,
        IMessageBusRegistry registry)
    {
        _registry = registry;

        Services = services;
    }

    public async Task SendAsync(
        SentMessage message,
        CancellationToken cancellationToken = default)
    {
        //Extract the current activity context
        var activity = Activity.Current;
        var headers = new Dictionary<string, string>();
        if (activity is not null)
        {
            DistributedContextPropagator.Current.Inject(activity, headers, (c, key, value) =>
            {
                var headers = (Dictionary<string, string>)c!;
                headers[key] = value;
            });
        }

        //Add that context to the message
        message = message with
        {
            Headers = message.Headers.AddRange(headers),
        };

        var logicalName = message.LogicalDestinationName;

        await _registry.OnReady;

        if (!_registry.TryGetBusId(logicalName, out var busId))
            ThrowForPublisher(logicalName);

        if (!_registry.TryGetBus(busId, out var bus))
            ThrowForPublisher(logicalName);

        if (!bus.TryGetPublisher(logicalName, out var publisher))
            ThrowForPublisher(logicalName);

        await publisher.Channel.WriteAsync(message, cancellationToken);
    }

    [DoesNotReturn]
    private void ThrowForPublisher(
        string logicalName)
        => throw new InvalidOperationException($"No publisher registered for {logicalName}");
}
