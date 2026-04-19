using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class EmailNotificationSender : NotificationChannelSender<EmailMethod, EmailEnvelope>
{
    private readonly IEmailSender _sender;

    public EmailNotificationSender(
        [ServiceKey] object serviceKey,
        IServiceProvider serviceProvider)
    {
        _sender = serviceProvider.GetRequiredKeyedService<IEmailSender>(serviceKey);
    }

    public override async Task SendAsync(
        EmailEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        await _sender.SendAsync(envelope.Message, cancellationToken);
    }
}
