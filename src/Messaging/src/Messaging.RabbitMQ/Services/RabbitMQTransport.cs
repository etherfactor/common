using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EtherGizmos.Common.Services;

internal sealed class RabbitMQTransport :
    IMessageListenerFactory,
    IMessagePublisherFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConnectionFactory _connectionFactory;

    public RabbitMQTransport(
        IServiceProvider serviceProvider,
        ConnectionFactory connectionFactory)
    {
        _serviceProvider = serviceProvider;
        _connectionFactory = connectionFactory;
    }

    public IMessageListenerTransport CreateListenerForQueue(
        string logicalName,
        string queue)
    {
        var logger = _serviceProvider
            .GetRequiredService<ILogger<RabbitMQListener>>();

        return new RabbitMQListener(
            logger,
            _connectionFactory,
            queue);
    }

    public IMessageListenerTransport CreateListenerForTopic(
        string logicalName,
        string topic,
        string subscription)
    {
        var logger = _serviceProvider
            .GetRequiredService<ILogger<RabbitMQListener>>();

        return new RabbitMQListener(
            logger,
            _connectionFactory,
            topic,
            subscription);
    }

    public IMessagePublisherTransport CreatePublisherForQueue(
        string logicalName,
        string queue)
    {
        var logger = _serviceProvider
            .GetRequiredService<ILogger<RabbitMQPublisher>>();

        return new RabbitMQPublisher(
            logger,
            _connectionFactory,
            queue);
    }

    public IMessagePublisherTransport CreatePublisherForTopic(
        string logicalName,
        string topic)
    {
        var logger = _serviceProvider
            .GetRequiredService<ILogger<RabbitMQPublisher>>();

        return new RabbitMQPublisher(
            logger,
            _connectionFactory,
            topic,
            subscription: string.Empty);
    }
}
