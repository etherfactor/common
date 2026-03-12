namespace EtherGizmos.Common;

public record NotificationChannelMetadata(
    string ChannelKey,
    string DisplayName,
    Type ConfigType,
    string ConfigSchema);
