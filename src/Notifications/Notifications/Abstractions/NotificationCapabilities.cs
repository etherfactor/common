using System.Text.Json.Nodes;

namespace EtherGizmos.Common.Abstractions;

public class NotificationCapabilities
{
    public required IReadOnlyList<NotificationEventCapability> Events { get; set; }

    public required IReadOnlyList<NotificationChannelCapability> Channels { get; set; }

    public required IReadOnlyList<NotificationScheduleCapability> Schedules { get; set; }
}

public class NotificationChannelCapability
{
    public required string ChannelKey { get; set; }

    public required string DisplayName { get; set; }

    public required JsonNode ConfigSchema { get; set; }
}

public class NotificationScheduleCapability
{
    public required string ScheduleKey { get; set; }

    public required string DisplayName { get; set; }

    public required JsonNode ConfigSchema { get; set; }
}

public class NotificationEventCapability
{
    public required string EventKey { get; set; }

    public required string DisplayName { get; set; }

    public required IReadOnlyList<NotificationChannelScheduleCapability> Supports { get; set; }
}

public class NotificationChannelScheduleCapability
{
    public required string ChannelKey { get; set; }

    public required string ScheduleKey { get; set; }
}
