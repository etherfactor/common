using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common.Services;

internal class DomainEventEmitter : IDomainEventEmitter
{
    private readonly IOptionsMonitor<NotificationTypeOptions> _typeOptions;
    private readonly IDomainEventSerializer _eventSerializer;
    private readonly IMessageSender _messageSender;

    public DomainEventEmitter(
        IOptionsMonitor<NotificationTypeOptions> typeOptions,
        IDomainEventSerializer eventSerializer,
        IMessageSender messageSender)
    {
        _typeOptions = typeOptions;
        _eventSerializer = eventSerializer;
        _messageSender = messageSender;
    }

    public async Task EmitAsync(
        IDomainEvent @event,
        IEnumerable<AudienceKey> audiences,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(@event);

        var eventType = _typeOptions.CurrentValue.EventTypeMap
            .FirstOrDefault(e => e.Value == @event.GetType())
            .Key
            ?? throw new InvalidOperationException($"Unknown event type '{@event.GetType()}'");

        var serialized = _eventSerializer.Serialize(@event);
        var message = new DomainEventMessage()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            EventType = eventType,
            Audiences = [.. audiences],
            PayloadType = serialized.Type,
            Payload = serialized.Payload,
        };

        await _messageSender.SendAsync(
            NotificationConstants.DomainEventsLogicalName,
            message,
            cancellationToken: cancellationToken);
    }
}
