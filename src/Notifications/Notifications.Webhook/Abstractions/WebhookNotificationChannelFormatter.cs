using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public abstract class WebhookNotificationChannelFormatter<TMode, TModel>
    : NotificationChannelFormatter<TMode, WebhookMethod, TModel, WebhookEnvelope>
    where TMode : DeliveryMode
    where TModel : class
{
    public override INotificationEnvelope<WebhookMethod> Format(
        Notification notification,
        TModel model)
    {
        return new WebhookEnvelope("application/json", notification.Payload);
    }
}
