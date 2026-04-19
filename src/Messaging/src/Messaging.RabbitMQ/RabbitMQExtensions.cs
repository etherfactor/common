using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
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
        public bool IsRabbitMQ(
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
            @this.Services.TryAddSingleton<RabbitMQTransportBuilder>();
            @this.Services.TryAddSingleton<IMessageListenerFactoryBuilder<RabbitMQOptions>>(provider => provider.GetRequiredService<RabbitMQTransportBuilder>());
            @this.Services.TryAddSingleton<IMessagePublisherFactoryBuilder<RabbitMQOptions>>(provider => provider.GetRequiredService<RabbitMQTransportBuilder>());

            ModularConfigurationTypeRegistry.Register<RootRabbitMQOptions, MessagingConnectionOptions>(
                sectionName: "Connections",
                itemIdName: "ConnectionId",
                typeName: ConnectionType.MessageBroker);

            return @this;
        }
    }
}
