using MimeKit;

namespace EtherGizmos.Common.Services;

internal interface ISmtpClientAdapter : IDisposable
{
    Task AuthenticateAsync(string username, string password, CancellationToken cancellationToken);

    Task ConnectAsync(string host, int port, bool useSsl, CancellationToken cancellationToken);

    Task SendAsync(MimeMessage message, CancellationToken cancellationToken);
}
