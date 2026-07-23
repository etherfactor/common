namespace EtherGizmos.Common.Abstractions;

public interface INotificationLockingCoordinator
{
    Task<IReadOnlyList<NotificationClaim>> ClaimBatchAsync(
        NotificationScheduleRef schedule,
        int maxCount = 100,
        CancellationToken cancellationToken = default);

    Task<NotificationClaim?> ClaimSingleAsync(
        long notificationId,
        CancellationToken cancellationToken = default);

    Task MarkFailedAsync(
        NotificationClaim claim,
        Exception exception,
        CancellationToken cancellationToken = default);

    Task MarkSentAsync(
        NotificationClaim claim,
        CancellationToken cancellationToken = default);
}
