using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class NotificationsServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        public IServiceCollection AddNotifications(
            string busId,
            Action<INotificationBuilder> configureNotifications)
        {
            @this.AddNotificationsCore();

            @this.TryAddEnumerable(new ServiceDescriptor(typeof(IInterceptor), typeof(NotificationSaveChangesInterceptor)));

            @this
                .AddMessaging(busId, (opt, conf) =>
                {
                    opt.Publishers.AddQueue(NotificationConstants.DomainEventsLogicalName, "domain-events");
                    opt.Listeners.AddQueue(NotificationConstants.DomainEventsLogicalName, "domain-events");

                    opt.Publishers.AddQueue(NotificationConstants.NotificationsLogicalName, "notifications");
                    opt.Listeners.AddQueue(NotificationConstants.NotificationsLogicalName, "notifications");
                })
                .AddConsumersFromAssemblies(typeof(NotificationConstants).Assembly!);

            var builder = new NotificationBuilder(@this);
            configureNotifications(builder);

            return @this;
        }

        internal IServiceCollection AddNotificationsCore()
        {
            @this.TryAddSingleton<IDomainEventEmitter, DomainEventEmitter>();
            @this.TryAddSingleton<IDomainEventSerializer, DomainEventSerializer>();
            return @this;
        }
    }
}
