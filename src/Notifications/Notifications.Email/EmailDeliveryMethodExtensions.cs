using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class EmailDeliveryMethodExtensions
{
    extension(DeliveryMethods)
    {
        public static EmailMethod Email => EmailMethod.Instance;
    }
}
