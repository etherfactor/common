using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace EtherGizmos.Common;

public static class RabbitMQExtensions
{
    extension(MessagingConstants)
    {
        public static string RabbitMQMessagingKey => "messaging-rabbitmq";
    }

    extension(IMessagingBuilder @this)
    {
        public IMessagingBuilder UseRabbitMQ(
            Action<RabbitMQMessagingOptions, IConfiguration> configureOptions)
        {
            @this.Services
                .AddOptions<RabbitMQMessagingOptions>()
                .Configure(configureOptions);

            @this.Services
                .AddSingleton<RabbitMQTransport>()
                .AddSingleton<IMessagePublisherFactory>(e => e.GetRequiredService<RabbitMQTransport>())
                .AddSingleton<IMessageListenerFactory>(e => e.GetRequiredService<RabbitMQTransport>());

            @this.Services
                .AddKeyedSingleton(MessagingConstants.RabbitMQMessagingKey, (provider, _) =>
                {
                    var options = provider
                        .GetRequiredService<IOptions<RabbitMQMessagingOptions>>()
                        .Value;

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
