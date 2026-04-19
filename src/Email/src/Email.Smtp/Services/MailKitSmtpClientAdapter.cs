using MailKit.Net.Smtp;
using MimeKit;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common.Services;

[ExcludeFromCodeCoverage]
internal class MailKitSmtpClientAdapter : ISmtpClientAdapter
{
    private readonly SmtpClient _client = new();

    public Task ConnectAsync(
        string host,
        int port,
        bool useSsl,
        CancellationToken cancellationToken)
        => _client.ConnectAsync(host, port, useSsl, cancellationToken);

    public Task AuthenticateAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
        => _client.AuthenticateAsync(username, password, cancellationToken);

    public Task SendAsync(
        MimeMessage message,
        CancellationToken cancellationToken)
        => _client.SendAsync(message, cancellationToken);

    public void Dispose()
        => _client.Dispose();
}
