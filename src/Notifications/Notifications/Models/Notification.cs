using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Models;

public class Notification : IEntity
{
    public long Id { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? SentAt { get; set; }

    public Guid EventId { get; set; }

    public long NotificationSubscriptionId { get; set; }

    public NotificationSubscription NotificationSubscription { get; set; } = null!;

    public string PayloadType { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public NotificationStatusType StatusType { get; set; }

    public int AttemptCount { get; set; }
}
