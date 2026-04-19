using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationHandler : INotificationHandler
{
    private readonly ILogger _logger;
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly INotificationSender _sender;

    public ImmediateNotificationHandler(
        ILogger<ImmediateNotificationHandler> logger,
        IUnitOfWorkFactory uowFactory,
        INotificationSender sender)
    {
        _logger = logger;
        _uowFactory = uowFactory;
        _sender = sender;
    }

    public async Task HandleAsync(
        long notificationId,
        CancellationToken cancellationToken = default)
    {
        using var uow = _uowFactory.Create();
        var notificationRepo = uow.Repository<Notification>();

        var notification = await notificationRepo.Data
            .Include(e => e.NotificationSubscription)
            .SingleOrDefaultAsync(e => e.Id == notificationId, cancellationToken: cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning("The notification {NotificationId} does not exist", notificationId);
            return;
        }

        if (notification.StatusType != NotificationStatusType.Pending)
        {
            _logger.LogWarning("The notification {NotificationId} has a status of {StatusType} and is not available for delivery",
                notificationId, notification.StatusType);
            return;
        }

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
            if (count == 0) return;

            await _sender.SendAsync(notification, cancellationToken);

            notification.StatusType = NotificationStatusType.Sent;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId}", notificationId);
            if (notification.AttemptCount < 10) notification.StatusType = NotificationStatusType.Pending;
            else notification.StatusType = NotificationStatusType.Failed;
            throw;
        }
        finally
        {
            await uow.SaveChangesAsync(cancellationToken);
        }
    }
}
