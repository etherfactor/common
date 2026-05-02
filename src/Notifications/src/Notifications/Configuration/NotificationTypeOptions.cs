using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Configuration;

public class NotificationTypeOptions
{
    public Dictionary<string, Type> EventTypeMap { get; set; } = [];

    public Dictionary<NotificationSchedule, Dictionary<NotificationChannel, Type>> FormatterMap { get; set; } = [];
}
