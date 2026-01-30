using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
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
            @this.AddChannel<EmailMethod, EmailNotificationSender>(NotificationChannelType.Email);

            @this.Services.TryAddKeyedTransient(NotificationChannelType.Email, (provider, _) =>
            {
                var resolver = provider.GetRequiredService<IConnectionResolver>();
                return resolver.GetEmailConnection(connectionId);
            });

            @this.Services
                .AddNotifications(opt =>
                {
                    opt.AddEmailChannel("MySmtpServer");

                    opt.AddNotification<PackageDelivered>("package.delivered", type =>
                    {
                        type.SupportsEmail<PackageDelivered, PackageDeliveredEmailFormatter>();
                        type.SupportsEmailDigest<PackageDelivered, PackageDeliveredEmailDigestFormatter>();
                    });
                });

            return @this;
        }
    }

    public class PackageDelivered { }
    public class PackageDeliveredEmailFormatter : IEmailNotificationChannelFormatter<ImmediateMode, PackageDelivered> { }
    public class PackageDeliveredEmailDigestFormatter:IEmailNotificationChannelFormatter<DigestMode, Digest<PackageDelivered>> { }

    extension<TModel>(INotificationTypeBuilder<TModel> @this)
        where TModel : class
    {
        public INotificationTypeBuilder<TModel> SupportsEmail<TFormatter>()
            where TFormatter : class, IEmailNotificationChannelFormatter<ImmediateMode, TModel>
        {
            return @this.Supports<TModel, EmailMethod, TFormatter>(DeliveryMethods.Email);
        }

        public INotificationTypeBuilder<TModel> SupportsEmailDigest<TFormatter>()
            where TFormatter : class, IEmailNotificationChannelFormatter<DigestMode, Digest<TModel>>
        {
            return @this.SupportsDigest<TModel, EmailMethod, TFormatter>(DeliveryMethods.Email);
        }
    }
}
