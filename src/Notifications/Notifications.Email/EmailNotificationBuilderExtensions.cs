using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class EmailNotificationBuilderExtensions
{
    extension(INotificationBuilder @this)
    {
        public INotificationBuilder AddEmailChannel(
            string connectionId)
        {
            @this.AddChannel<EmailNotificationSender>(NotificationChannelType.Email);
            @this.Services.TryAddKeyedTransient(NotificationChannelType.Email, (provider, _) =>
            {
                var resolver = provider.GetRequiredService<IConnectionResolver>();
                return resolver.GetEmailConnection(connectionId);
            });

            //@this.Services
            //    .AddNotifications(opt =>
            //    {
            //        opt.AddChannel<EmailNotificationSender>(NotificationChannelType.Email);

            //        opt.AddNotification<PackageDelivered>("package.delivered", type =>
            //        {
            //            type.Supports<PackageDeliveredEmailFormatter>(NotificationChannelType.Email);
            //        });
            //    });

            return @this;
        }
    }
}
