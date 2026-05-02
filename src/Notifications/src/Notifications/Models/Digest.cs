using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Models;

public class Digest<TNotification>
    : IDomainEvent
    where TNotification : class
{
    public DateTimeOffset StartAt { get; set; }

    public DateTimeOffset EndAt { get; set; }

    public List<TNotification> Notifications { get; set; } = [];
}
