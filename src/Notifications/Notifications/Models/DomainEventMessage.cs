using EtherGizmos.Common.Abstractions;
using System.Text.Json;

namespace EtherGizmos.Common.Models;

public class DomainEventMessage
{
    public Guid EventId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string EventType { get; set; } = null!;

    public string PayloadType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DomainEventMessage() { }

    public DomainEventMessage(
        IDomainEvent @event)
    {
        PayloadType = @event.GetType().FullName!;
        Payload = JsonSerializer.Serialize(@event, JsonSerializerOptions.Default);
    }
}
