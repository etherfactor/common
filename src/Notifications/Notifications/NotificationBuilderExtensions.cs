using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Text.Json;
using System.Text.Json.Schema;

namespace EtherGizmos.Common;

public static class NotificationBuilderExtensions
{
    extension(INotificationBuilder @this)
    {
        public INotificationBuilder AddChannel<TChannel>(
            string channelType,
            Action<INotificationChannelBuilder>? configureChannel = null)
            where TChannel : class, INotificationChannelSender
        {
            @this.Services.TryAddKeyedScoped<INotificationChannelSender, TChannel>(channelType);

            if (configureChannel is not null)
            {
                var builder = new NotificationChannelBuilder(channelType, @this.Services);
                configureChannel(builder);
            }

            return @this;
        }

        public INotificationBuilder AddNotification<TNotification>(
            string eventType,
            Action<INotificationTypeBuilder> configureType)
        {
            var builder = new NotificationTypeBuilder(eventType, @this.Services);
            configureType(builder);

            return @this;
        }
    }

    extension(INotificationChannelBuilder @this)
    {
        public INotificationChannelBuilder HasConfiguration<TModel>()
            where TModel : class
        {
            @this.Services.AddOptions<NotificationChannelOptions>(@this.ChannelType)
                .Configure(opt =>
                {
                    opt.ConfigurationSchema = JsonSchemaExporter
                        .GetJsonSchemaAsNode(JsonSerializerOptions.Web, typeof(TModel))
                        .ToJsonString();
                });

            return @this;
        }
    }

    extension(INotificationTypeBuilder @this)
    {
        public INotificationTypeBuilder Supports<TFormatter>(
            string eventType)
            where TFormatter : class, INotificationChannelFormatter
        {
            @this.Services.AddOptions<NotificationTypeOptions>(@this.EventType)
                .Configure(opt =>
                {
                    opt.FormatterMap.Add(eventType, typeof(TFormatter));
                });

            return @this;
        }
    }
}
