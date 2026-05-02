using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public record NotificationClaim(
    long NotificationId,
    Notification Notification,
    Guid LockId);
