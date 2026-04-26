using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Hosting;

namespace EtherGizmos.Common.Services;

internal class NotificationCollectorHostedService : IHostedService
{
    private readonly List<INotificationCollector> _collectors;
    private readonly CancellationTokenSource _cts = new();

    public NotificationCollectorHostedService(
        IEnumerable<INotificationCollector> collectors)
    {
        _collectors = [.. collectors];
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);
        foreach (var collector in _collectors)
        {
            _ = collector.CollectAsync(cts.Token);
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cts.Cancel();
        return Task.CompletedTask;
    }
}
