using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationCollector : NotificationCollector
{
    private readonly ILogger _logger;
    private readonly INotificationDispatcher _sender;
    private readonly INotificationLockingCoordinator _coordinator;

    public override TimeSpan Delay => TimeSpan.FromMinutes(5);

    public ImmediateNotificationCollector(
        ILogger<ImmediateNotificationCollector> logger,
        INotificationDispatcher sender,
        INotificationLockingCoordinator coordinator)
        : base(logger)
    {
        _logger = logger;
        _sender = sender;
        _coordinator = coordinator;
    }

    protected override async Task CollectBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var claims = await _coordinator.ClaimBatchAsync(NotificationSchedules.Immediate, cancellationToken: cancellationToken);

        var exceptions = new List<Exception>();
        foreach (var claim in claims)
        {
            using var activity = ActivitySources.Notifications.StartActivityFromCarrier(
                $"Send notification {claim.NotificationId} via {claim.Notification.Subscription.ChannelId}",
                ActivityKind.Consumer,
                claim.Notification.Headers.AsReadOnly());

            activity?.SetTag("notification.schedule", NotificationSchedules.Immediate.Id);
            activity?.SetTag("notification.subscription.id", claim.Notification.SubscriptionId);
            activity?.SetTag("notification.channel", claim.Notification.Subscription.ChannelId);

            try
            {
                await _sender.DispatchAsync(claim.Notification, cancellationToken);
                await _coordinator.MarkSentAsync(claim, cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send notification {NotificationId}", claim.NotificationId);
                await _coordinator.MarkFailedAsync(claim, ex, cancellationToken: cancellationToken);

                exceptions.Add(ex);
            }
        }

        if (exceptions.Any())
        {
            _logger.LogWarning("Failed to send {ErrorCount} notifications", exceptions.Count);
        }
    }
}
