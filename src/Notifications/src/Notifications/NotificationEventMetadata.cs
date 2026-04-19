using System.Collections.Immutable;

namespace EtherGizmos.Common;

public record NotificationEventMetadata(
    string EventType,
    string DisplayName,
    ImmutableHashSet<NotificationEventDeliveryMetadata> Supports);

public record NotificationEventDeliveryMetadata(
    NotificationScheduleMetadata Schedule,
    NotificationChannelMetadata Channel);
