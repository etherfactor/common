using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationDispatcher
{
    Task SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
