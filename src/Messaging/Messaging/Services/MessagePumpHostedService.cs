using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common.Services;

public class MessagePumpHostedService : IHostedService
{
    private readonly MessagingOptions _options;
    private readonly IMessageBus _bus;

    public MessagePumpHostedService(
        IOptions<MessagingOptions> options,
        IMessageBus bus)
    {
        _options = options.Value;
        _bus = bus;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken)
    {
        await _bus.StartAsync(cancellationToken);

        foreach (var listener in _options.Listeners)
        {
            if (_options.Serverless)
                throw new InvalidOperationException("Cannot have listeners configured in a serverless environment.");

            var logicalName = listener.Key;
            var config = listener.Value;

            if (config.IsTopic)
            {
                await _bus.RegisterListenerForTopicAsync(
                    logicalName, topic: config.Name, subscription: config.Subscription!, cancellationToken: cancellationToken);
            }
            else
            {
                await _bus.RegisterListenerForQueueAsync(
                    logicalName, queue: config.Name, cancellationToken: cancellationToken);
            }
        }

        foreach (var publisher in _options.Publishers)
        {
            var logicalName = publisher.Key;
            var config = publisher.Value;

            if (config.IsTopic)
            {
                await _bus.RegisterPublisherForTopicAsync(
                    logicalName, topic: config.Name, cancellationToken: cancellationToken);
            }
            else
            {
                await _bus.RegisterPublisherForQueueAsync(
                    logicalName, queue: config.Name, cancellationToken: cancellationToken);
            }
        }
    }

    public async Task StopAsync(
        CancellationToken cancellationToken)
    {
        await _bus.StopAsync(cancellationToken);
    }
}
