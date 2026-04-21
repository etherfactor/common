namespace EtherGizmos.Common.Abstractions;

public record WebhookChannel() : NotificationChannel("webhook")
{
    public static WebhookChannel Instance { get; } = new();
}
