namespace EtherGizmos.Common;

public record RegisteredNotificationSchedule(
    string ScheduleKey,
    string DisplayName,
    Type ConfigType,
    string ConfigSchema);
