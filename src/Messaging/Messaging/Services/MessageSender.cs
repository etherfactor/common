using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class MessageSender : IMessageSender
{
    private readonly IMessageBus _bus;

    public IServiceProvider Services { get; }

    public MessageSender(
        IServiceProvider services,
        IMessageBus bus)
    {
        _bus = bus;

        Services = services;
    }

    public async Task SendAsync(
        SentMessage message,
        CancellationToken cancellationToken = default)
    {
        var logicalName = message.LogicalDestinationName;

        if (!_bus.TryGetPublisher(logicalName, out var publisher))
        {
            throw new InvalidOperationException($"No publisher registered for {logicalName}");
        }

        await publisher.Channel.WriteAsync(message, cancellationToken);
    }
}
