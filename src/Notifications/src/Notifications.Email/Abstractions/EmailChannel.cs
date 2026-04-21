namespace EtherGizmos.Common.Abstractions;

public sealed record EmailChannel() : NotificationChannel("email")
{
    public static EmailChannel Instance { get; } = new();
}
