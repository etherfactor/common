using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Abstractions;

public interface IMessagePublisherFactoryBuilder<TOptions>
    where TOptions : MessagingConnectionOptions
{
    IMessagePublisherFactory CreatePublisherFactory(
        string busId,
        TOptions connection);
}
