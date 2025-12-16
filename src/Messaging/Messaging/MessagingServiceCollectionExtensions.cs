using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtherGizmos.Common;

public static class MessagingServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        public IMessagingBuilder AddMessaging(
            Action<MessagingOptions, IConfiguration> configureOptions)
        {
            return @this.AddMessaging(string.Empty, configureOptions);
        }

        public IMessagingBuilder AddMessaging(
            string busId,
            Action<MessagingOptions, IConfiguration> configureOptions)
        {
            @this.AddMessagingCore();

            @this.AddOptions<MessagingOptions>(busId)
                .Configure(configureOptions);

            var key = new BusKey(busId);
            @this.TryAddKeyedSingleton<IMessageBus, MessageBus>(key);

            @this.AddHostedService<MessagePumpHostedService>();
            @this.AddOptions<MessageBusOptions>()
                .Configure(opt =>
                {
                    opt.Buses.Add(busId);
                });

            return new MessagingBuilder(busId, @this);
        }

        private IServiceCollection AddMessagingCore()
        {
            @this.TryAddSingleton<IMessageBusRegistry, MessageBusRegistry>();

            @this.TryAddSingleton<IMessageReceiver, MessageReceiver>();
            @this.TryAddSingleton<IMessageSender, MessageSender>();

            @this.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

            @this.AddOptions<JsonSerializerOptions>(MessagingConstants.OptionsName)
                .Configure(opt =>
                {
                    opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    opt.Converters.Add(new JsonStringEnumConverter());
                });

            return @this;
        }
    }
}
