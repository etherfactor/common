using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class NotificationsServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        public IServiceCollection AddNotifications(
            string databaseConnectionId,
            string busId,
            Action<INotificationBuilder> configureNotifications)
        {
            @this.AddNotificationsCore(databaseConnectionId);

            @this.TryAddEnumerable(new ServiceDescriptor(
                typeof(IInterceptor),
                typeof(NotificationSaveChangesInterceptor),
                ServiceLifetime.Singleton));

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

        internal IServiceCollection AddNotificationsCore(
            string databaseConnectionId)
        {
            @this.TryAddSingleton<IDomainEventEmitter, DomainEventEmitter>();
            @this.TryAddSingleton<IDomainEventSerializer, DomainEventSerializer>();

            @this
                .AddDbContext<NotificationContext>((provider, opt) =>
                {
                    opt.UseConnection(provider, databaseConnectionId, opt =>
                    {
                        opt.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery);
                    });
                });

            @this
                .AddUnitOfWork(opt =>
                {
                    opt.BindDbContext<NotificationContext>();
                });

            @this.AddMigrations("Notification", typeof(NotificationsServiceCollectionExtensions).Assembly!)
                .UseConnection(databaseConnectionId);

            return @this;
        }
    }
}
