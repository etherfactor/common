using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Models;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class NotificationBuilderExtensions
{
    extension(INotificationBuilder @this)
    {
        public INotificationBuilder AddChannel<TChannel, TSender>(
            string displayName,
            Type channelConfigType)
            where TChannel : NotificationChannel
            where TSender : class, INotificationChannelSender<TChannel>
        {
            var method = Activator.CreateInstance<TChannel>()!;
            NotificationRegistry.RegisterChannel(method.Key, displayName, channelConfigType);

            @this.Services.TryAddKeyedScoped<INotificationChannelSender, TSender>(method.Key);

            return @this;
        }

        public INotificationBuilder AddNotification<TNotification, TRouter>(
            string eventType,
            Action<INotificationEventBuilder<TNotification>> configureType)
            where TNotification : class, IDomainEvent
            where TRouter : class, IDomainEventRouter<TNotification>
        {
            var builder = new NotificationEventBuilder<TNotification>(eventType, @this.Services);
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

    extension<TModel>(INotificationEventBuilder<TModel> @this)
        where TModel : class, IDomainEvent
    {
        public INotificationEventBuilder<TModel> HasDisplayName(
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

        public INotificationEventBuilder<TModel> Supports<TChannel, TFormatter>()
            where TChannel : NotificationChannel
            where TFormatter : class, INotificationChannelFormatter<ImmediateSchedule, TChannel, TModel>
        {
            var method = Activator.CreateInstance<TChannel>()!;

            @this.Services.AddKeyedSingleton<INotificationChannelFormatter, TFormatter>((method.Key, typeof(TModel)));

            @this.Services.AddOptions<NotificationTypeOptions>(@this.EventType)
                .Configure(opt =>
                {
                    opt.FormatterMap[NotificationSchedules.Immediate] ??= [];
                    opt.FormatterMap[NotificationSchedules.Immediate][method] = typeof(TFormatter);
                });

            @this.Services
                .AddOptions<NotificationEventOptions>()
                .Configure(opt =>
                {
                    var schedule = NotificationRegistry.GetSchedule(NotificationSchedules.Immediate.Key);
                    var channel = NotificationRegistry.GetChannel(method.Key);
                    opt.Metadata.AddOrUpdate(
                        @this.EventType,
                        _ => new(@this.EventType, @this.EventType, [new(schedule, channel)]),
                        (_, current) => current with { Supports = current.Supports.Add(new(schedule, channel)) });
                });

            return @this;
        }

        public INotificationEventBuilder<TModel> SupportsDigest<TChannel, TFormatter>()
            where TChannel : NotificationChannel
            where TFormatter : class, INotificationChannelFormatter<DigestSchedule, TChannel, Digest<TModel>>
        {
            var method = Activator.CreateInstance<TChannel>()!;

            @this.Services.AddKeyedSingleton<INotificationChannelFormatter, TFormatter>((method.Key, typeof(Digest<TModel>)));

            @this.Services.AddOptions<NotificationTypeOptions>(@this.EventType)
                .Configure(opt =>
                {
                    opt.FormatterMap[NotificationSchedules.Digest] ??= [];
                    opt.FormatterMap[NotificationSchedules.Digest][method] = typeof(TFormatter);
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
                    var schedule = NotificationRegistry.GetSchedule(NotificationSchedules.Digest.Key);
                    var channel = NotificationRegistry.GetChannel(method.Key);
                    opt.Metadata.AddOrUpdate(
                        @this.EventType,
                        _ => new(@this.EventType, @this.EventType, [new(schedule, channel)]),
                        (_, current) => current with { Supports = current.Supports.Add(new(schedule, channel)) });
                });

            return @this;
        }
    }
}
