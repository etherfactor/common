using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace EtherGizmos.Common.Services;

internal class NotificationLockingCoordinator : INotificationLockingCoordinator
{
    private readonly IUnitOfWorkFactory _uowFactory;

    public NotificationLockingCoordinator(
        IUnitOfWorkFactory uowFactory)
    {
        _uowFactory = uowFactory;
    }

    public async Task<IReadOnlyList<NotificationClaim>> ClaimBatchAsync(
        NotificationSchedule schedule,
        int maxCount = 100,
        CancellationToken cancellationToken = default)
    {
        using var uow = _uowFactory.Create();
        var notificationRepo = uow.Repository<Notification>();

        var lockId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var lockedUntil = now.Add(TimeSpan.FromSeconds(30));

        var immediate = NotificationSchedules.Immediate.Key;
        var candidateIds = await notificationRepo.Data
            .Where(e =>
                e.NotificationSubscription.ScheduleType == schedule.Key
                && (
                    e.Status == NotificationStatusType.Pending
                    || (
                        e.Status == NotificationStatusType.InFlight
                        && e.LockedUntil < now
                    )
                )
                && e.AttemptCount < 10)
            .OrderBy(e => e.Id)
            .Select(e => e.Id)
            .Take(maxCount)
            .ToListAsync(cancellationToken: cancellationToken);

        var claimed = await TryLockAsync(notificationRepo, candidateIds, lockId, now, lockedUntil, cancellationToken);
        return claimed;
    }

    public async Task<NotificationClaim?> ClaimSingleAsync(
        long notificationId,
        CancellationToken cancellationToken = default)
    {
        using var uow = _uowFactory.Create();
        var notificationRepo = uow.Repository<Notification>();

        var lockId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var lockedUntil = now.Add(TimeSpan.FromSeconds(30));

        var claimed = await TryLockAsync(notificationRepo, [notificationId], lockId, now, lockedUntil, cancellationToken);
        return claimed.SingleOrDefault();
    }

    public async Task MarkFailedAsync(
        NotificationClaim claim,
        Exception exception,
        CancellationToken cancellationToken = default)
    {
        using var uow = _uowFactory.Create();
        var notificationRepo = uow.Repository<Notification>();

        await notificationRepo.Data
            .Where(e => e.Id == claim.NotificationId
                && e.LockId == claim.LockId)
            .ExecuteUpdateAsync(e => e
                .SetProperty(e => e.Status, e => e.AttemptCount < 10
                    ? NotificationStatusType.Pending
                    : NotificationStatusType.Failed)
                .SetProperty(e => e.LastError, _ => exception.ToString())
                .SetProperty(e => e.LockId, _ => null)
                .SetProperty(e => e.LockedBy, _ => null)
                .SetProperty(e => e.LockedUntil, _ => null),
                cancellationToken: cancellationToken);
    }

    public async Task MarkSentAsync(
        NotificationClaim claim,
        CancellationToken cancellationToken = default)
    {
        using var uow = _uowFactory.Create();
        var notificationRepo = uow.Repository<Notification>();

        await notificationRepo.Data
            .Where(e => e.Id == claim.NotificationId
                && e.LockId == claim.LockId)
            .ExecuteUpdateAsync(e => e
                .SetProperty(e => e.Status, _ => NotificationStatusType.Sent)
                .SetProperty(e => e.SentAt, _ => DateTimeOffset.UtcNow)
                .SetProperty(e => e.LastError, _ => null)
                .SetProperty(e => e.LockId, _ => null)
                .SetProperty(e => e.LockedBy, _ => null)
                .SetProperty(e => e.LockedUntil, _ => null),
                cancellationToken: cancellationToken);
    }

    private async Task<IReadOnlyList<NotificationClaim>> TryLockAsync(
        IRepository<Notification> notificationRepo,
        List<long> candidateIds,
        Guid lockId,
        DateTimeOffset now,
        DateTimeOffset lockedUntil,
        CancellationToken cancellationToken = default)
    {
        await notificationRepo.Data
            .Where(e =>
                candidateIds.Contains(e.Id)
                && (e.LockedUntil == null || e.LockedUntil < now)
                && e.AttemptCount < 10)
            .ExecuteUpdateAsync(e => e
                .SetProperty(e => e.Status, _ => NotificationStatusType.InFlight)
                .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                .SetProperty(e => e.LastAttemptAt, _ => now)
                .SetProperty(e => e.LockId, _ => lockId)
                .SetProperty(e => e.LockedBy, _ => Environment.MachineName)
                .SetProperty(e => e.LockedUntil, _ => lockedUntil),
                cancellationToken: cancellationToken);

        var claimed = await notificationRepo.Data
            .AsNoTracking()
            .Where(e => e.LockId == lockId)
            .ToListAsync(cancellationToken: cancellationToken);

        return [.. claimed.Select(e => new NotificationClaim(e.Id, e, lockId))];
    }
}
