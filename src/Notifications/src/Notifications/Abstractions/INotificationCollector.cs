namespace EtherGizmos.Common.Abstractions;

public interface INotificationCollector
{
    Task CollectAsync(
        CancellationToken cancellationToken = default);
}
