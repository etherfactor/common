using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Hosting;

namespace Notifications.Test;

internal class EventHostedService : BackgroundService
{
    private readonly IDomainEventEmitter _emitter;

    public EventHostedService(
        IDomainEventEmitter emitter)
    {
        _emitter = emitter;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            await _emitter
                .EmitAsync(new TestDomainEvent()
                {
                    Value = new Random().Next(),
                },
                [new("test", "123")],
                stoppingToken);

            await Task.Delay(5000, stoppingToken);
        }
    }
}
