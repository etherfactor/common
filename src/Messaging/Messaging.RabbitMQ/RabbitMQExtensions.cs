using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RabbitMQ.Client;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common;

public static class RabbitMQExtensions
{
    extension(MessagingConstants)
    {
        public static string RabbitMQMessagingKey => "messaging-rabbitmq";
    }

    extension(MessagingConnectionOptions @this)
    {
        public bool IsPostgreSql(
            [NotNullWhen(true)] out RabbitMQOptions? options)
        {
            if (@this is RabbitMQOptions typed)
            {
                options = typed;
                return true;
            }

            options = null;
            return false;
        }
    }

    extension(IConnectionResolverBuilder @this)
    {
        public IConnectionResolverBuilder WithRabbitMQ()
        {
            @this.Services.TryAddSingleton<IDbConnectionFactory<PostgreSqlOptions>, PostgreSqlDbConnectionFactory>();

            ModularConfigurationTypeRegistry.Register<RootPostgreSqlOptions, DatabaseConnectionOptions>(
                sectionName: "Connections",
                itemIdName: "ConnectionId",
                typeName: ConnectionType.Database);

            return @this;
        }
    }

    extension(IMessagingBuilder @this)
    {
        public IMessagingBuilder UseRabbitMQ(
            string connectionId)
        {
            @this.Services
                .AddSingleton<RabbitMQTransport>()
                .AddSingleton<IMessagePublisherFactory>(e => e.GetRequiredService<RabbitMQTransport>())
                .AddSingleton<IMessageListenerFactory>(e => e.GetRequiredService<RabbitMQTransport>());

            @this.Services
                .AddKeyedSingleton(MessagingConstants.RabbitMQMessagingKey, (provider, _) =>
                {
                    var resolver= provider.GetRequiredService<IConnectionResolver>();

                    var connection=resolver.GetMessagingConnection(connectionId);

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
                });

            return @this;
        }
    }
}
