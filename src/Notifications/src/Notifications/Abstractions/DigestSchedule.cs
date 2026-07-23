namespace EtherGizmos.Common.Abstractions;

public sealed record DigestSchedule() : NotificationScheduleRef("digest")
{
    public static DigestSchedule Instance { get; } = new();
}
