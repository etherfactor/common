using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class EmailNotificationSender : NotificationChannelSender<EmailEnvelope>
{
    private readonly IEmailSender _sender;

    public override string ChannelKey => NotificationChannelType.Email;

    public EmailNotificationSender(
        [ServiceKey] object serviceKey,
        IServiceProvider serviceProvider)
    {
        _sender = serviceProvider.GetRequiredKeyedService<IEmailSender>(serviceKey);
    }

    protected override async Task SendTypedAsync(
        EmailEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        await _sender.SendAsync(envelope.Message, cancellationToken);
    }
}
