using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using MimeKit;

namespace EtherGizmos.Common.Services;

internal class SmtpEmailSender : IEmailSender
{
    private readonly SmtpEmailOptions _options;
    private readonly ISmtpClientAdapterFactory _factory;

    public SmtpEmailSender(
        SmtpEmailOptions options,
        ISmtpClientAdapterFactory factory)
    {
        _options = options;
        _factory = factory;
    }

    public async Task SendAsync(
        EmailMessage message,
        CancellationToken cancellationToken = default)
    {
        using var client = _factory.Create();
        await client.ConnectAsync(_options.Host, _options.Port, _options.UseSsl, cancellationToken);

        if (_options.Username is not null && _options.Password is not null)
        {
            await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
        }

        var mime = BuildMimeMessage(message);
        await client.SendAsync(mime, cancellationToken);
    }

    internal static MimeMessage BuildMimeMessage(EmailMessage message)
    {
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
            mime.Body = new TextPart("html") { Text = message.HtmlBody };
        }
        else
        {
            mime.Body = new TextPart("plain") { Text = message.TextBody ?? string.Empty };
        }

        return mime;
    }
}
