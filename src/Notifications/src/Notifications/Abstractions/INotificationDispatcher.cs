using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationDispatcher
{
    Task DispatchAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
