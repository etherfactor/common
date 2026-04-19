using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Hosting;

namespace EtherGizmos.Common.Services;

public class OutboxHostedService : BackgroundService
{
    private readonly IOutboxMessagePublisher _publisher;
    private readonly IOutboxSignal _signal;

    public OutboxHostedService(
        IOutboxMessagePublisher publisher,
        IOutboxSignal signal)
    {
        _publisher = publisher;
        _signal = signal;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        var attemptsSincePublish = 0;
        while (!stoppingToken.IsCancellationRequested)
        {
            var delay = ComputeDelay(attemptsSincePublish);
            var delayTask = Task.Delay(delay, stoppingToken);
            var pulseTask = _signal.WaitAsync(stoppingToken);

            await Task.WhenAny(delayTask, pulseTask);

            var claimed = await _publisher.PublishAsync(stoppingToken);

            if (claimed) attemptsSincePublish = 0;
            else attemptsSincePublish++;
        }
    }

    private static TimeSpan ComputeDelay(
        int attemptsSincePublish)
    {
        return attemptsSincePublish switch
        {
            <= 0 => TimeSpan.FromSeconds(1),
            1 => TimeSpan.FromSeconds(2),
            2 => TimeSpan.FromSeconds(5),
            3 => TimeSpan.FromSeconds(15),
            4 => TimeSpan.FromMinutes(30),
            _ => TimeSpan.FromMinutes(60),
        };
    }
}
