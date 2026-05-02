using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class DigestNotificationHandler : INotificationHandler
{
    public Task HandleAsync(
        long notificationId,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask; //Intentional no-op, the collector will batch these for us
}
