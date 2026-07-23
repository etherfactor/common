namespace EtherGizmos.Common.Abstractions;

public record WebhookChannel() : NotificationChannelRef("webhook")
{
    public static WebhookChannel Instance { get; } = new();
}
