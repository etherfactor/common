using EtherGizmos.Common.Converters;
using EtherGizmos.Common.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtherGizmos.Common.Abstractions;

public abstract class WebhookNotificationChannelFormatter<TMode, TModel>
    : NotificationChannelFormatter<TMode, WebhookChannel, WebhookEnvelope, TModel>
    where TMode : NotificationScheduleRef
    where TModel : class
{
    private static readonly JsonSerializerOptions _jsonOptions;

    static WebhookNotificationChannelFormatter()
    {
        _jsonOptions = new()
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new ObjectToInferredTypesConverter(),
            },
        };
    }

    public override WebhookEnvelope Format(
        Notification notification,
        TModel model)
    {
        var config = notification.Subscription.ChannelConfig.As<WebhookChannelConfig>();
        return new WebhookEnvelope(config.Method, config.Endpoint, "application/json", notification.Payload);
    }
}
