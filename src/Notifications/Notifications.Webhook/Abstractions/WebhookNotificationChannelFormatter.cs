using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public abstract class WebhookNotificationChannelFormatter<TMode, TModel>
    : NotificationChannelFormatter<TMode, WebhookMethod, WebhookEnvelope, TModel>
    where TMode : DeliveryMode
    where TModel : class
{
    public override WebhookEnvelope Format(
        Notification notification,
        TModel model)
    {
        var config = (WebhookChannelConfig)notification.NotificationSubscription.ChannelConfig;
        return new WebhookEnvelope(config.Method, config.Endpoint, "application/json", notification.Payload);
    }
}
