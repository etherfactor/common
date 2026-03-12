using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

public static class WebhookNotificationBuilderExtensions
{
    extension(INotificationBuilder @this)
    {
        public INotificationBuilder AddWebhookChannel()
        {
            @this.AddChannel<WebhookMethod, WebhookNotificationSender>("Webhook", typeof(WebhookChannelConfig));

            @this.Services.AddHttpClient(DeliveryMethods.Webhook.Key);

            return @this;
        }
    }
}
