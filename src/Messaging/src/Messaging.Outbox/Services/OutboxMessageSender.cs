using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using System.Diagnostics;

namespace EtherGizmos.Common.Services;

internal class OutboxMessageSender : IMessageSender
{
    private readonly IMessageSender _inner;
    private readonly IOutboxSignal _signal;
    private readonly IUnitOfWorkFactory _uowFactory;

    public IServiceProvider Services => _inner.Services;

    public OutboxMessageSender(
        IMessageSender inner,
        IOutboxSignal signal,
        IUnitOfWorkFactory uowFactory)
    {
        _inner = inner;
        _signal = signal;
        _uowFactory = uowFactory;
    }

    public async Task SendAsync(
        SentMessage message,
        CancellationToken cancellationToken = default)
    {
        using var activity = ActivitySources.Messaging.StartActivityFromCarrier(
            $"Enqueue {message.Type} for {message.LogicalDestinationName}",
            ActivityKind.Producer,
            message.Headers);

        activity?.SetTag("messaging.operation.name", "enqueue");
        activity?.SetTag("messaging.destination.name", message.LogicalDestinationName);
        activity?.SetTag("messaging.message.type", message.Type);
        activity?.SetTag("messaging.outbox.enabled", true);

        message = message.AddActivityHeaders(activity);

        using var uow = _uowFactory.Create();
        var messageRepo = uow.Repository<OutboxMessage>();

        var messageRecord = new OutboxMessage()
        {
            MessageId = Guid.NewGuid().ToString("N"),
            QueuedAt = DateTimeOffset.UtcNow,
            AvailableAt = DateTimeOffset.UtcNow,
            Status = OutboxStatusType.Pending,
            LogicalDestinationName = message.LogicalDestinationName,
            Type = message.Type,
            Payload = message.Body,
            Headers = message.Headers.ToDictionary(),
        };

        messageRepo.Add(messageRecord);

        await uow.SaveChangesAsync(cancellationToken);

        _signal.Pulse();
    }
}
