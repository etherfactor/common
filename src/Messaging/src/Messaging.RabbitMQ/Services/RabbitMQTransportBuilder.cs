using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using RabbitMQ.Client;

namespace EtherGizmos.Common.Services;

internal class RabbitMQTransportBuilder : IMessageListenerFactoryBuilder<RabbitMQOptions>, IMessagePublisherFactoryBuilder<RabbitMQOptions>
{
    private readonly IServiceProvider _serviceProvider;

    public RabbitMQTransportBuilder(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IMessageListenerFactory CreateListenerFactory(
        string busId,
        RabbitMQOptions connection)
    {
        var connectionFactory = BuildConnectionFactory(connection);
        return new RabbitMQTransport(_serviceProvider, connectionFactory);
    }

    public IMessagePublisherFactory CreatePublisherFactory(
        string busId,
        RabbitMQOptions connection)
    {
        var connectionFactory = BuildConnectionFactory(connection);
        return new RabbitMQTransport(_serviceProvider, connectionFactory);
    }

    private ConnectionFactory BuildConnectionFactory(
        RabbitMQOptions options)
    {
        var factory = new ConnectionFactory();
        if (options.ConnectionString is not null)
        {
            factory.Uri = new Uri(options.ConnectionString);
        }
        else
        {
            factory.HostName = options.Host ?? "localhost";
            factory.UserName = options.Username!;
            factory.Password = options.Password!;
            factory.Port = options.Port;
        }

        factory.AutomaticRecoveryEnabled = true;
        factory.TopologyRecoveryEnabled = true;

        return factory;
    }
}
