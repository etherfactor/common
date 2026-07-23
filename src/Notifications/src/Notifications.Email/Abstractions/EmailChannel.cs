namespace EtherGizmos.Common.Abstractions;

public sealed record EmailChannel() : NotificationChannelRef("email")
{
    public static EmailChannel Instance { get; } = new();
}
