using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Services;

internal class SmtpEmailSenderFactory : IEmailSenderFactory<SmtpEmailOptions>
{
    public IEmailSender Create(
        SmtpEmailOptions options)
    {
        return new SmtpEmailSender(options);
    }
}
