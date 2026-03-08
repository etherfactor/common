namespace EtherGizmos.Common.Abstractions;

public class WebhookEnvelope
    : INotificationEnvelope<WebhookMethod>
{
    public string ContentType { get; set; }

    public string Payload { get; set; }

    public WebhookEnvelope(
        string contentType,
        string payload)
    {
        ContentType = contentType;
        Payload = payload;
    }
}
