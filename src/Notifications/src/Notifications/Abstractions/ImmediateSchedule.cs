namespace EtherGizmos.Common.Abstractions;

public sealed record ImmediateSchedule() : NotificationScheduleRef("immediate")
{
    public static ImmediateSchedule Instance { get; } = new();
}
