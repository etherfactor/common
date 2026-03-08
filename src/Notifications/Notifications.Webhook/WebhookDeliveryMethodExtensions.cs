using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class WebhookDeliveryMethodExtensions
{
    extension(DeliveryMethods)
    {
        public static WebhookMethod Webhook => WebhookMethod.Instance;
    }
}
