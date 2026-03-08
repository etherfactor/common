using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Models;

public class DomainEventMessage
{
    public Guid EventId { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string EventType { get; set; } = null!;

    public List<AudienceKey> Audiences { get; set; } = [];

    public bool IsDerived { get; set; }

    public string PayloadType { get; set; } = null!;

    public string Payload { get; set; } = null!;
}
