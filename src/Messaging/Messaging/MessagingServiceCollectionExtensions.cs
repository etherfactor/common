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
            @this.AddOptions<MessagingOptions>()
                .Configure(configureOptions);

            @this.AddHostedService<MessagePumpHostedService>();

            @this.TryAddSingleton<IMessageBus, MessageBus>();
            @this.TryAddSingleton<IMessageReceiver, MessageReceiver>();
            @this.TryAddSingleton<IMessageSender, MessageSender>();

            @this.TryAddSingleton<IMessageSerializer, JsonMessageSerializer>();

            @this.AddOptions<JsonSerializerOptions>(MessagingConstants.OptionsName)
                .Configure(opt =>
                {
                    opt.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    opt.Converters.Add(new JsonStringEnumConverter());
                });

            return new MessagingBuilder(@this);
        }
    }
}
