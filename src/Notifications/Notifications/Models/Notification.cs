using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Models;

public class Notification : IEntity
{
    public long Id { get; set; }

    public long NotificationSubscriptionId { get; set; }

    public NotificationSubscription NotificationSubscription { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public NotificationStatusType StatusType { get; set; }

    public int AttemptCount { get; set; }
}
