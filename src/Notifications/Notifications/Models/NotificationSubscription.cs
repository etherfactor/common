namespace EtherGizmos.Common.Models;

public class NotificationSubscription
{
    public long Id { get; set; }

    //TODO: Make this generic, to allow other user id types
    public Guid UserId { get; set; }

    public int EventTypeId { get; set; }

    public int ChannelTypeId { get; set; }

    public string ScheduleType { get; set; } = null!;

    public string? ScheduleConfig { get; set; }

    public bool IsEnabled { get; set; }
}
