using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationCollector : NotificationCollector
{
    private readonly ILogger _logger;
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly INotificationSender _sender;

    public override TimeSpan Delay => TimeSpan.FromMinutes(5);

    public ImmediateNotificationCollector(
        ILogger<ImmediateNotificationCollector> logger,
        IUnitOfWorkFactory uowFactory,
        INotificationSender sender)
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

        var immediate = DeliveryModes.Immediate.Key;
        var notifications = await notificationRepo.Data
            .Where(e => e.NotificationSubscription.ScheduleType == immediate
                && e.StatusType == NotificationStatusType.Pending
                && e.AttemptCount < 10)
            .Include(e => e.NotificationSubscription)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (var notification in notifications)
        {
            try
            {
                var count = await notificationRepo.Data
                    .Where(e => e.Id == notification.Id
                        && e.StatusType == NotificationStatusType.Pending)
                    .ExecuteUpdateAsync(e => e
                        .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                        .SetProperty(e => e.StatusType, _ => NotificationStatusType.InFlight),
                        cancellationToken: cancellationToken);

                //Something else may have tried to send the notification
                if (count == 0) continue;

                await _sender.SendAsync(notification, cancellationToken);

                notification.StatusType = NotificationStatusType.Sent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {NotificationId}", notification.Id);
                if (notification.AttemptCount < 10) notification.StatusType = NotificationStatusType.Pending;
                else notification.StatusType = NotificationStatusType.Failed;
            }
            finally
            {
                await uow.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
