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
            @this.AddOptions<MessagingOptions>(busId)
                .Configure(configureOptions);

            @this.AddHostedService<MessagePumpHostedService>();
            @this.AddOptions<MessageBusOptions>()
                .Configure(opt =>
                {
                    opt.Buses.Add(busId);
                });

            @this.TryAddKeyedSingleton<IMessageBus, MessageBus>(busId);

            @this.TryAddKeyedSingleton<IMessageReceiver, MessageReceiver>(busId);
            @this.TryAddKeyedSingleton<IMessageSender, MessageSender>(busId);

            @this.TryAddKeyedSingleton<IMessageSerializer, JsonMessageSerializer>(busId);

            @this.AddOptions<JsonSerializerOptions>($"{MessagingConstants.OptionsName}:{busId}")
                .Configure(opt =>
                {
                    opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    opt.Converters.Add(new JsonStringEnumConverter());
                });

            return new MessagingBuilder(busId, @this);
        }
    }
}
