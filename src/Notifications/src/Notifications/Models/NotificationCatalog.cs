using System.Collections.Immutable;

namespace EtherGizmos.Common.Models;

public record NotificationCatalog
{
    public required ImmutableList<NotificationEvent> Events { get; init; }

    public required ImmutableList<NotificationChannel> Channels { get; init; }

    public required ImmutableList<NotificationSchedule> Schedules { get; init; }
}
