namespace EtherGizmos.Common.Services;

internal class MailKitSmtpClientAdapterFactory : ISmtpClientAdapterFactory
{
    public ISmtpClientAdapter Create()
        => new MailKitSmtpClientAdapter();
}
