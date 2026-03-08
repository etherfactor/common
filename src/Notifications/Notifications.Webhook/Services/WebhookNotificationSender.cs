using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class WebhookNotificationSender : NotificationChannelSender<WebhookMethod>
{
    private readonly IHttpClientFactory _httpClientFactory;

    public WebhookNotificationSender(
        IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public override async Task SendAsync(
        INotificationEnvelope<WebhookMethod> envelope,
        CancellationToken cancellationToken = default)
    {
        var typed = (WebhookEnvelope)envelope;

        var content = new StringContent(typed.Payload);
        content.Headers.ContentType = new(typed.ContentType);

        using var client = _httpClientFactory.CreateClient(WebhookMethod.Instance.Key);
        await client.PostAsync("https://localhost:52227/webhook/post", content, cancellationToken);
    }
}
