using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Configuration;

public class NotificationTypeOptions
{
    public Dictionary<string, Type> EventTypeMap { get; set; } = [];

    public Dictionary<DeliveryMode, Dictionary<DeliveryMethod, Type>> FormatterMap { get; set; } = [];
}
