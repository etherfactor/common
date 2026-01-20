using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using MailKit.Net.Smtp;
using MimeKit;

namespace EtherGizmos.Common.Services;

internal class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;

    public SmtpEmailSender(
        SmtpEmailOptions options)
    {
        _options = options;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, cancellationToken);

        if (_options.Username is not null && _options.Password is not null)
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        var mime = new MimeMessage();

        if (message.From != EmailAddress.Empty)
        {
            mime.From.Add(new MailboxAddress(message.From.Name, message.From.Address));
        }

        foreach (var address in message.To.Where(e => e != EmailAddress.Empty))
        {
            mime.To.Add(new MailboxAddress(address.Name, address.Address));
        }

        foreach (var address in message.Cc.Where(e => e != EmailAddress.Empty))
        {
            mime.Cc.Add(new MailboxAddress(address.Name, address.Address));
        }

        foreach (var address in message.Bcc.Where(e => e != EmailAddress.Empty))
        {
            mime.Bcc.Add(new MailboxAddress(address.Name, address.Address));
        }

        mime.Subject = message.Subject;

        if (message.HtmlBody is not null)
        {
            mime.Body = new TextPart("html")
            {
                Text = message.HtmlBody,
            };
        }
        else
        {
            mime.Body = new TextPart("plain")
            {
                Text = message.TextBody ?? string.Empty,
            };
        }

        await client.SendAsync(mime, cancellationToken);
    }
}
