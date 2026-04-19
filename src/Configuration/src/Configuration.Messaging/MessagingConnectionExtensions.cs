using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common;

public static class MessagingConnectionExtensions
{
    extension(ConnectionType)
    {
        public static string MessageBroker => "MessageBroker";
    }

    extension(IConnectionResolver @this)
    {
        public MessagingConnectionOptions GetMessagingConnection(
            string connectionId)
        {
            var connection = @this.GetOptions<ConnectionOptions, MessagingConnectionOptions>(connectionId, ConnectionType.MessageBroker);
            return connection;
        }
    }
}
