using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationSender
{
    Task SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default);
}
