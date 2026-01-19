using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelFormatter
{
    string ChannelKey { get; }

    INotificationEnvelope Format(
        Notification notification);
}
