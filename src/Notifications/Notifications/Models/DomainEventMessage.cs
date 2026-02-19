namespace EtherGizmos.Common.Models;

public class DomainEventMessage
{
    public Guid EventId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string EventType { get; set; } = null!;

    public string PayloadType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DomainEventMessage() { }
}
