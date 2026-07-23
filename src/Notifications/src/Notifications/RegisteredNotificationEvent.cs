using System.Collections.Immutable;

namespace EtherGizmos.Common;

public record RegisteredNotificationEvent(
    string EventKey,
    string DisplayName,
    Type ConfigType,
    string ConfigSchema,
    ImmutableHashSet<RegisteredNotificationEventDelivery> Supports);

public record RegisteredNotificationEventDelivery(
    RegisteredNotificationSchedule Schedule,
    RegisteredNotificationChannel Channel);
