using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Configuration;

public class NotificationTypeOptions
{
    public Dictionary<string, Type> EventTypeMap { get; set; } = [];

    public Dictionary<string, Type> EventConfigMap { get; set; } = [];

    public Dictionary<NotificationScheduleRef, Dictionary<NotificationChannelRef, Type>> FormatterMap { get; set; } = [];
}
