using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Models;

public class Notification : IEntity
{
    public long Id { get; set; }

    public Guid EventId { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? SentAt { get; set; }

    public long NotificationSubscriptionId { get; set; }

    public NotificationSubscription NotificationSubscription { get; set; } = null!;

    public bool IsDerived { get; set; }

    public string PayloadType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public NotificationStatusType StatusType { get; set; }

    public int AttemptCount { get; set; }
}
