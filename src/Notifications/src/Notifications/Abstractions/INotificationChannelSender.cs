namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelSender
{
    Task SendAsync(
        INotificationEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public interface INotificationChannelSender<TChannel>
    : INotificationChannelSender
    where TChannel : NotificationChannelRef;
