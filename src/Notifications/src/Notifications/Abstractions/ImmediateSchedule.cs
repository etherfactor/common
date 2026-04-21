namespace EtherGizmos.Common.Abstractions;

public sealed record ImmediateSchedule() : NotificationSchedule("immediate")
{
    public static ImmediateSchedule Instance { get; } = new();
}
