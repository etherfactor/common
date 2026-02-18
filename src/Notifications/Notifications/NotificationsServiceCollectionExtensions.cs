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
            string connectionId,
            Action<INotificationBuilder> configureNotifications)
        {
            @this.TryAddEnumerable(new ServiceDescriptor(typeof(IInterceptor), typeof(NotificationSaveChangesInterceptor)));

            @this
                .AddMessaging(NotificationConstants.BusId, (opt, conf) =>
                {
                    opt.Publishers.AddQueue("domain-events", "domain-events");
                    opt.Publishers.AddQueue("notifications", "notifications");
                })
                .AddConsumersFromAssemblies(typeof(NotificationConstants).Assembly!)
                .UseConnection(connectionId);

            var builder = new NotificationBuilder(@this);
            configureNotifications(builder);

            return @this;
        }
    }
}
