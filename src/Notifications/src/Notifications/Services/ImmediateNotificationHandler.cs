using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationHandler : INotificationHandler
{
    private readonly ILogger _logger;
    private readonly INotificationDispatcher _sender;
    private readonly INotificationLockingCoordinator _coordinator;

    public ImmediateNotificationHandler(
        ILogger<ImmediateNotificationHandler> logger,
        INotificationDispatcher sender,
        INotificationLockingCoordinator coordinator)
    {
        _logger = logger;
        _sender = sender;
        _coordinator = coordinator;
    }

    public async Task HandleAsync(
        long notificationId,
        CancellationToken cancellationToken = default)
    {
        var claim = await _coordinator.ClaimSingleAsync(notificationId, cancellationToken: cancellationToken);
        if (claim is null)
        {
            _logger.LogWarning("Failed to claim notification {NotificationId}", notificationId);
            return;
        }

        using var activity = ActivitySources.Notifications.StartActivityFromCarrier(
            $"Send notification {claim.NotificationId} via {claim.Notification.NotificationSubscription.ChannelKey}",
            ActivityKind.Consumer,
            claim.Notification.Headers.AsReadOnly());

        try
        {
            await _sender.DispatchAsync(claim.Notification, cancellationToken);
            await _coordinator.MarkSentAsync(claim, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification {NotificationId}", notificationId);
            await _coordinator.MarkFailedAsync(claim, ex, cancellationToken: cancellationToken);
            throw;
        }
    }
}
