using System.Collections.Immutable;

namespace EtherGizmos.Common;

public record RegisteredNotificationEvent(
    string EventType,
    string DisplayName,
    ImmutableHashSet<RegisteredNotificationEventDelivery> Supports);

public record RegisteredNotificationEventDelivery(
    RegisteredNotificationSchedule Schedule,
    RegisteredNotificationChannel Channel);
