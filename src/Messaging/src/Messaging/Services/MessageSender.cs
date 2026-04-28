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
        message = message.AddActivityHeaders(Activity.Current);

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
