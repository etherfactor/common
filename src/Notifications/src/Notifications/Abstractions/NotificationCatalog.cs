using System.Text.Json.Nodes;

namespace EtherGizmos.Common.Abstractions;

public class NotificationCatalog
{
    public required IReadOnlyList<NotificationCatalogEvent> Events { get; set; }

    public required IReadOnlyList<NotificationCatalogChannel> Channels { get; set; }

    public required IReadOnlyList<NotificationCatalogSchedule> Schedules { get; set; }
}

public class NotificationCatalogChannel
{
    public required string ChannelKey { get; set; }

    public required string DisplayName { get; set; }

    public required JsonNode ConfigSchema { get; set; }
}

public class NotificationCatalogSchedule
{
    public required string ScheduleKey { get; set; }

    public required string DisplayName { get; set; }

    public required JsonNode ConfigSchema { get; set; }
}

public class NotificationCatalogEvent
{
    public required string EventKey { get; set; }

    public required string DisplayName { get; set; }

    public required IReadOnlyList<NotificationCatalogChannelSchedule> Supports { get; set; }
}

public class NotificationCatalogChannelSchedule
{
    public required string ChannelKey { get; set; }

    public required string ScheduleKey { get; set; }
}
