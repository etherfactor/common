using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class EmailNotificationSender : NotificationChannelSender<EmailMethod>
{
    private readonly IEmailSender _sender;

    public EmailNotificationSender(
        [ServiceKey] object serviceKey,
        IServiceProvider serviceProvider)
    {
        _sender = serviceProvider.GetRequiredKeyedService<IEmailSender>(serviceKey);
    }

    public override async Task SendAsync(
        INotificationEnvelope<EmailMethod> envelope,
        CancellationToken cancellationToken = default)
    {
        //TODO: Fix this, I don't like it
        await _sender.SendAsync(((EmailEnvelope)envelope).Message, cancellationToken);
    }
}
