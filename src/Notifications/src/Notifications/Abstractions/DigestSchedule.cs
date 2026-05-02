namespace EtherGizmos.Common.Abstractions;

public sealed record DigestSchedule() : NotificationSchedule("digest")
{
    public static DigestSchedule Instance { get; } = new();
}
