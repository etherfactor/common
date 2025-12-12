using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class RabbitMQTransport : IMessageListenerFactory, IMessagePublisherFactory
{
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQTransport(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IMessageListener CreateListenerForQueue(string logicalName, string queue)
    {
        return new RabbitMQListener(_serviceProvider, queue);
    }

    public IMessageListener CreateListenerForTopic(string logicalName, string topic, string subscription)
    {
        return new RabbitMQListener(_serviceProvider, topic, subscription);
    }

    public IMessagePublisher CreatePublisherForQueue(string logicalName, string queue)
    {
        return new RabbitMQPublisher(_serviceProvider, queue);
    }

    public IMessagePublisher CreatePublisherForTopic(string logicalName, string topic)
    {
        return new RabbitMQPublisher(_serviceProvider, topic, "");
    }
}
