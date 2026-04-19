using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Abstractions;

public interface IMessageListenerFactoryBuilder<TOptions>
    where TOptions : MessagingConnectionOptions
{
    IMessageListenerFactory CreateListenerFactory(
        string busId,
        TOptions connection);
}
