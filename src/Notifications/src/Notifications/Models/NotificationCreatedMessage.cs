namespace EtherGizmos.Common.Models;

public class NotificationCreatedMessage
{
    public long NotificationId { get; set; }

    public string ScheduleType { get; set; } = null!;
}
