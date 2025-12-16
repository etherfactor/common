using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common.Services;

public class MessagePumpHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptions<MessageBusOptions> _busOptions;
    private readonly IOptionsMonitor<MessagingOptions> _options;

    public MessagePumpHostedService(
        IServiceProvider serviceProvider,
        IOptions<MessageBusOptions> busOptions,
        IOptionsMonitor<MessagingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _busOptions = busOptions;
        _options = options;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        foreach (var busId in _busOptions.Value.Buses)
        {
            var key = new BusKey(busId);
            var bus = _serviceProvider.GetRequiredKeyedService<IMessageBus>(key);

            var options = _options.Get(busId);

            await bus.StartAsync(cancellationToken);

            foreach (var listener in options.Listeners)
            {
                if (options.Serverless)
                    throw new InvalidOperationException("Cannot have listeners configured in a serverless environment.");

                var logicalName = listener.Key;
                var config = listener.Value;

                if (config.IsTopic)
                {
                    await bus.RegisterListenerForTopicAsync(
                        logicalName, topic: config.Name, subscription: config.Subscription!, cancellationToken: cancellationToken);
                }
                else
                {
                    await bus.RegisterListenerForQueueAsync(
                        logicalName, queue: config.Name, cancellationToken: cancellationToken);
                }
            }

            foreach (var publisher in options.Publishers)
            {
                var logicalName = publisher.Key;
                var config = publisher.Value;

                if (config.IsTopic)
                {
                    await bus.RegisterPublisherForTopicAsync(
                        logicalName, topic: config.Name, cancellationToken: cancellationToken);
                }
                else
                {
                    await bus.RegisterPublisherForQueueAsync(
                        logicalName, queue: config.Name, cancellationToken: cancellationToken);
                }
            }
        }
    }

    public async Task StopAsync(
        CancellationToken cancellationToken)
    {
        foreach (var busId in _busOptions.Value.Buses)
        {
            var key = new BusKey(busId);
            var bus = _serviceProvider.GetRequiredKeyedService<IMessageBus>(key);
            await bus.StopAsync(cancellationToken);
        }
    }
}
