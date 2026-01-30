using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Configuration;

public class NotificationTypeOptions
{
    public Dictionary<DeliveryMode, Dictionary<DeliveryMethod, Type>> FormatterMap { get; set; } = [];
}
