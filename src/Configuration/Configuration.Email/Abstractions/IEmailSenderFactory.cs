using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Abstractions;

public interface IEmailSenderFactory<TOptions>
    where TOptions : EmailConnectionOptions, new()
{
    IEmailSender Create(
        TOptions options);
}
