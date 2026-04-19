namespace EtherGizmos.Common.Services;

internal interface ISmtpClientAdapterFactory
{
    ISmtpClientAdapter Create();
}
