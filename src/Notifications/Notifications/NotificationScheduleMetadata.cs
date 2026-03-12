namespace EtherGizmos.Common;

public record NotificationScheduleMetadata(
    string ScheduleKey,
    string DisplayName,
    Type ConfigType,
    string ConfigSchema);
