namespace EtherGizmos.Common.Abstractions;

public record WebhookMethod() : DeliveryMethod("webhook")
{
    public static WebhookMethod Instance { get; } = new();
}
