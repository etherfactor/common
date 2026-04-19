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
            string displayName,
            Type channelConfigType,
            Action<INotificationChannelBuilder>? configureChannel = null)
            where TMethod : DeliveryMethod
            where TChannel : class, INotificationChannelSender<TMethod>
        {
            var method = Activator.CreateInstance<TMethod>()!;
            NotificationMetadata.RegisterChannel(method.Key, displayName, channelConfigType);

            @this.Services.TryAddKeyedScoped<INotificationChannelSender, TChannel>(method.Key);

            if (configureChannel is not null)
            {
                var builder = new NotificationChannelBuilder(method.Key, @this.Services);
                configureChannel(builder);
            }

            return @this;
        }

        public INotificationBuilder AddNotification<TNotification, TRouter>(
            string eventType,
            Action<INotificationTypeBuilder<TNotification>> configureType)
            where TNotification : class, IDomainEvent
            where TRouter : class, IDomainEventRouter<TNotification>
        {
            var builder = new NotificationTypeBuilder<TNotification>(eventType, @this.Services);
            configureType(builder);

            @this.Services.TryAddSingleton<IDomainEventRouter<TNotification>, TRouter>();
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
        where TModel : class, IDomainEvent
    {
        public INotificationTypeBuilder<TModel> HasDisplayName(
            string displayName)
        {
            @this.Services
                .AddOptions<NotificationEventOptions>()
                .Configure(opt =>
                {
                    opt.Metadata.AddOrUpdate(
                        @this.EventType,
                        _ => new(@this.EventType, displayName, []),
                        (_, current) => current with { DisplayName = displayName });
                });

            return @this;
        }

        public INotificationTypeBuilder<TModel> Supports<TMethod, TFormatter>()
            where TMethod : DeliveryMethod
            where TFormatter : class, INotificationChannelFormatter<ImmediateMode, TMethod, TModel>
        {
            var method = Activator.CreateInstance<TMethod>()!;

            @this.Services.AddKeyedSingleton<INotificationChannelFormatter, TFormatter>((method.Key, typeof(TModel)));

            @this.Services.AddOptions<NotificationTypeOptions>(@this.EventType)
                .Configure(opt =>
                {
                    opt.FormatterMap[DeliveryModes.Immediate] ??= [];
                    opt.FormatterMap[DeliveryModes.Immediate][method] = typeof(TFormatter);
                });

            @this.Services
                .AddOptions<NotificationEventOptions>()
                .Configure(opt =>
                {
                    var schedule = NotificationMetadata.GetSchedule(DeliveryModes.Immediate.Key);
                    var channel = NotificationMetadata.GetChannel(method.Key);
                    opt.Metadata.AddOrUpdate(
                        @this.EventType,
                        _ => new(@this.EventType, @this.EventType, [new(schedule, channel)]),
                        (_, current) => current with { Supports = current.Supports.Add(new(schedule, channel)) });
                });

            return @this;
        }

        public INotificationTypeBuilder<TModel> SupportsDigest<TMethod, TFormatter>()
            where TMethod : DeliveryMethod
            where TFormatter : class, INotificationChannelFormatter<DigestMode, TMethod, Digest<TModel>>
        {
            var method = Activator.CreateInstance<TMethod>()!;

            @this.Services.AddKeyedSingleton<INotificationChannelFormatter, TFormatter>((method.Key, typeof(Digest<TModel>)));

            @this.Services.AddOptions<NotificationTypeOptions>(@this.EventType)
                .Configure(opt =>
                {
                    opt.FormatterMap[DeliveryModes.Digest] ??= [];
                    opt.FormatterMap[DeliveryModes.Digest][method] = typeof(TFormatter);
                });

            //If we support digests, we need to be able to route them to the user that owns them. This service is
            //effectively a no-op, returning the user already listed on the notification
            @this.Services.TryAddEnumerable(new ServiceDescriptor(
                typeof(IDomainEventRouter<Digest<TModel>>),
                typeof(DigestRouter<TModel>),
                ServiceLifetime.Singleton));

            @this.Services
                .AddOptions<NotificationEventOptions>()
                .Configure(opt =>
                {
                    var schedule = NotificationMetadata.GetSchedule(DeliveryModes.Digest.Key);
                    var channel = NotificationMetadata.GetChannel(method.Key);
                    opt.Metadata.AddOrUpdate(
                        @this.EventType,
                        _ => new(@this.EventType, @this.EventType, [new(schedule, channel)]),
                        (_, current) => current with { Supports = current.Supports.Add(new(schedule, channel)) });
                });

            return @this;
        }
    }
}
