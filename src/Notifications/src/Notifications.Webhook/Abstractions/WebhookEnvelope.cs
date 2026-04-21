namespace EtherGizmos.Common.Abstractions;

public record WebhookEnvelope(
    string Method,
    string Endpoint,
    string ContentType,
    string Payload)
    : INotificationEnvelope<WebhookChannel>;
