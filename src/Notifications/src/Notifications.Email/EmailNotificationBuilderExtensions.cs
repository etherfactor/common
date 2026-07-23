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
            @this.AddChannel<EmailChannel, EmailNotificationSender>("Email", typeof(EmailChannelConfig));

            @this.Services.TryAddKeyedTransient(NotificationChannels.Email.Id, (provider, _) =>
            {
                var resolver = provider.GetRequiredService<IConnectionResolver>();
                return resolver.CreateEmailSender(connectionId);
            });

            return @this;
        }
    }
}
