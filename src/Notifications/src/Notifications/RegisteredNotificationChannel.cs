namespace EtherGizmos.Common;

public record RegisteredNotificationChannel(
    string ChannelKey,
    string DisplayName,
    Type ConfigType,
    string ConfigSchema);
