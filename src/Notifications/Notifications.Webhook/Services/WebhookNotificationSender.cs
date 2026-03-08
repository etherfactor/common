using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class WebhookNotificationSender : NotificationChannelSender<WebhookMethod, WebhookEnvelope>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookNotificationSender(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task SendAsync(
        WebhookEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        var content = new StringContent(envelope.Payload);
        content.Headers.ContentType = new(envelope.ContentType);

        using var client = _httpClientFactory.CreateClient(WebhookMethod.Instance.Key);
        await client.PostAsync("https://localhost:52227/webhook/post", content, cancellationToken);
    }
}
