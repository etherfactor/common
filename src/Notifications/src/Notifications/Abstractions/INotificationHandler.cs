namespace EtherGizmos.Common.Abstractions;

public interface INotificationHandler
{
    Task HandleAsync(
        long notificationId,
        CancellationToken cancellationToken = default);
}
