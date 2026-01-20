using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class NotificationsServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        public INotificationBuilder AddNotifications()
        {
            @this.TryAddEnumerable(new ServiceDescriptor(typeof(IInterceptor), typeof(NotificationSaveChangesInterceptor)));

            return new NotificationBuilder();
        }
    }
}
