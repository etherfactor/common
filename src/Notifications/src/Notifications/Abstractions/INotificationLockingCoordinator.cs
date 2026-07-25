using EtherGizmos.Common.Models;
using System.Linq.Expressions;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationLockingCoordinator
{
    Task<IReadOnlyList<NotificationClaim>> ClaimBatchAsync(
        NotificationScheduleRef schedule,
        int maxCount = 100,
        Expression<Func<Notification, bool>>? additionalCondition = null,
        CancellationToken cancellationToken = default);

    Task<NotificationClaim?> ClaimSingleAsync(
        long notificationId,
        CancellationToken cancellationToken = default);

    Task MarkReleasedAsync(
        NotificationClaim claim,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        NotificationClaim claim,
        Exception exception,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(
        NotificationClaim claim,
        CancellationToken cancellationToken = default);
}
