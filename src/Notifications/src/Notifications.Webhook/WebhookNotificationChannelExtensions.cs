using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class WebhookNotificationChannelExtensions
{
    extension(NotificationChannels)
    {
        public static WebhookChannel Webhook => WebhookChannel.Instance;
    }
}
