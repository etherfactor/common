using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Services;

internal class SmtpEmailSenderFactory : IEmailSenderFactory<SmtpEmailOptions>
{
    private readonly ISmtpClientAdapterFactory _adapterFactory;

    public SmtpEmailSenderFactory(
        ISmtpClientAdapterFactory adapterFactory)
    {
        _adapterFactory = adapterFactory;
    }

    public IEmailSender Create(
        SmtpEmailOptions options)
    {
        return new SmtpEmailSender(options, _adapterFactory);
    }
}
