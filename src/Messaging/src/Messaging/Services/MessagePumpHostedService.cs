using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common.Services;

public sealed class MessagePumpHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IMessageBusRegistry _busRegistry;
    private readonly IOptions<MessageBusOptions> _busOptions;
    private readonly IOptionsMonitor<MessagingOptions> _options;

    public MessagePumpHostedService(
        IServiceProvider serviceProvider,
        IMessageBusRegistry busRegistry,
        IOptions<MessageBusOptions> busOptions,
        IOptionsMonitor<MessagingOptions> options)
    {
        _serviceProvider = serviceProvider;
        _busRegistry = busRegistry;
        _busOptions = busOptions;
        _options = options;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var busId in _busOptions.Value.Buses)
        {
            var key = new BusKey(busId);
            var bus = _serviceProvider.GetRequiredKeyedService<IMessageBus>(key);
            var options = _options.Get(busId);

            await bus.StartAsync(cancellationToken).ConfigureAwait(false);

            foreach (var listener in options.Listeners)
            {
                if (options.Serverless)
                {
                    throw new InvalidOperationException(
                        "Cannot configure listeners in a serverless environment.");
                }

                var logicalName = listener.Key;
                var config = listener.Value;

                if (config.IsTopic)
                {
                    await bus.RegisterListenerForTopicAsync(
                        logicalName,
                        topic: config.Name,
                        subscription: config.Subscription!,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await bus.RegisterListenerForQueueAsync(
                        logicalName,
                        queue: config.Name,
                        cancellationToken).ConfigureAwait(false);
                }
            }

            foreach (var publisher in options.Publishers)
            {
                var logicalName = publisher.Key;
                var config = publisher.Value;

                if (config.IsTopic)
                {
                    await bus.RegisterPublisherForTopicAsync(
                        logicalName,
                        topic: config.Name,
                        cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    await bus.RegisterPublisherForQueueAsync(
                        logicalName,
                        queue: config.Name,
                        cancellationToken).ConfigureAwait(false);
                }
            }
        }

        _busRegistry.MarkReady();
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        // Stop in reverse order so later-started buses are torn down first.
        foreach (var busId in _busOptions.Value.Buses.Reverse())
        {
            try
            {
                var key = new BusKey(busId);
                var bus = _serviceProvider.GetRequiredKeyedService<IMessageBus>(key);
                await bus.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            //It's possible the service provider disposes (and stops) buses before this service can do so. This should
            //prevent any errors, should that occur
            catch (ObjectDisposedException) { }
        }
    }
}
