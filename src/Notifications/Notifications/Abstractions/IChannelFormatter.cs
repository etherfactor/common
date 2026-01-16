using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface IChannelFormatter
{
    int NotificationChannelDefinitionId { get; }

    IChannelEnvelope Format(
        Notification notification);
}
