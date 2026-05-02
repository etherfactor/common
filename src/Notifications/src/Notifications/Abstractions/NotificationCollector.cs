using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationCollector : INotificationCollector
{
    private readonly ILogger _logger;

    public abstract TimeSpan Delay { get; }

    public NotificationCollector(
        ILogger logger)
    {
        _logger = logger;
    }

    public async Task CollectAsync(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await CollectBatchAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Encountered an error while collecting notifications");
            }

            await Task.Delay(Delay, cancellationToken);
        }
    }

    protected abstract Task CollectBatchAsync(
        CancellationToken cancellationToken = default);
}
