using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationCollector : NotificationCollector
{
    private readonly ILogger _logger;
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly INotificationDispatcher _sender;

    public override TimeSpan Delay => TimeSpan.FromMinutes(5);

    public ImmediateNotificationCollector(
        ILogger<ImmediateNotificationCollector> logger,
        IUnitOfWorkFactory uowFactory,
        INotificationDispatcher sender)
        : base(logger)
    {
        _logger = logger;
        _uowFactory = uowFactory;
        _sender = sender;
    }

    protected override async Task CollectBatchAsync(
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
                e.NotificationSubscription.ScheduleType == immediate
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
            .Take(100)
            .ToListAsync(cancellationToken: cancellationToken);

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

        var exceptions = new List<Exception>();
        foreach (var notification in claimed)
        {
            try
            {
                await _sender.DispatchAsync(notification, cancellationToken);

                await notificationRepo.Data
                    .Where(e => e.Id == notification.Id
                        && e.LockId == lockId)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(e => e.Status, _ => NotificationStatusType.Sent)
                        .SetProperty(e => e.SentAt, _ => now)
                        .SetProperty(e => e.LastError, _ => null)
                        .SetProperty(e => e.LockId, _ => null)
                        .SetProperty(e => e.LockedBy, _ => null)
                        .SetProperty(e => e.LockedUntil, _ => null),
                        cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {NotificationId}", notification.Id);
                await notificationRepo.Data
                    .Where(e => e.Id == notification.Id
                        && e.LockId == lockId)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(e => e.Status, e => e.AttemptCount < 10
                            ? NotificationStatusType.Pending
                            : NotificationStatusType.Failed)
                        .SetProperty(e => e.LastError, _ => ex.ToString())
                        .SetProperty(e => e.LockId, _ => null)
                        .SetProperty(e => e.LockedBy, _ => null)
                        .SetProperty(e => e.LockedUntil, _ => null),
                        cancellationToken: cancellationToken);

                exceptions.Add(ex);
            }
        }

        if (exceptions.Any())
        {
            _logger.LogWarning("Failed to send {ErrorCount} notifications", exceptions.Count);
        }
    }
}
