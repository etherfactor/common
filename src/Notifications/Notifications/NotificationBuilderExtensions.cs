using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Models;
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
        public INotificationBuilder AddChannel<TMethod, TChannel>(
            string channelType,
            Action<INotificationChannelBuilder>? configureChannel = null)
            where TMethod : DeliveryMethod
            where TChannel : class, INotificationChannelSender<TMethod>
        {
            @this.Services.TryAddKeyedScoped<INotificationChannelSender<TMethod>, TChannel>(channelType);

            if (configureChannel is not null)
            {
                var builder = new NotificationChannelBuilder(channelType, @this.Services);
                configureChannel(builder);
            }

            return @this;
        }

        public INotificationBuilder AddNotification<TNotification>(
            string eventType,
            Action<INotificationTypeBuilder<TNotification>> configureType)
            where TNotification : class
        {
            var builder = new NotificationTypeBuilder<TNotification>(eventType, @this.Services);
            configureType(builder);

            @this.Services.AddOptions<NotificationTypeOptions>()
                .Configure(opt =>
                {
                    if (opt.EventTypeMap.TryGetValue(eventType, out var type))
                    {
                        if (type != typeof(TNotification))
                            throw new InvalidOperationException($"The event type '{eventType}' is already associated with type '{type}'");
                    }

                    opt.EventTypeMap[eventType] = typeof(TNotification);
                });

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

    extension<TModel>(INotificationTypeBuilder<TModel> @this)
        where TModel : class
    {
        public INotificationTypeBuilder<TModel> Supports<TMethod, TFormatter>(
            TMethod method)
            where TMethod : DeliveryMethod
            where TFormatter : class, INotificationChannelFormatter<ImmediateMode, TMethod, TModel>
        {
            @this.Services.AddSingleton<INotificationChannelFormatter<ImmediateMode, TMethod, TModel>, TFormatter>();
            @this.Services.AddOptions<NotificationTypeOptions>(@this.EventType)
                .Configure(opt =>
                {
                    opt.FormatterMap[DeliveryModes.Immediate] ??= [];
                    opt.FormatterMap[DeliveryModes.Immediate][method] = typeof(TFormatter);
                });

            return @this;
        }

        public INotificationTypeBuilder<TModel> SupportsDigest<TMethod, TFormatter>(
            TMethod method)
            where TMethod : DeliveryMethod
            where TFormatter : class, INotificationChannelFormatter<DigestMode, TMethod, Digest<TModel>>
        {
            @this.Services.AddSingleton<INotificationChannelFormatter<DigestMode, TMethod, Digest<TModel>>, TFormatter>();
            @this.Services.AddOptions<NotificationTypeOptions>(@this.EventType)
                .Configure(opt =>
                {
                    opt.FormatterMap[DeliveryModes.Digest] ??= [];
                    opt.FormatterMap[DeliveryModes.Digest][method] = typeof(TFormatter);
                });

            return @this;
        }
    }
}
