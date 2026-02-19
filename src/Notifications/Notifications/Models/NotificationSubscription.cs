using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Models;

public class NotificationSubscription : IEntity
{
    public long Id { get; set; }

    public string UserId { get; set; } = null!;

    public string EventType { get; set; } = null!;

    public string ChannelKey { get; set; } = null!;

    public string ScheduleType { get; set; } = null!;

    public string? ScheduleConfig { get; set; }

    public bool IsEnabled { get; set; }
}
