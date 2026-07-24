using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class WebhookNotificationSender : NotificationChannelSender<WebhookChannel, WebhookEnvelope>
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

        using var client = _httpClientFactory.CreateClient(WebhookChannel.Instance.Id);

        var request = new HttpRequestMessage(new HttpMethod(envelope.Method), envelope.Endpoint) { Content = content };
        await client.SendAsync(request, cancellationToken);
    }
}
