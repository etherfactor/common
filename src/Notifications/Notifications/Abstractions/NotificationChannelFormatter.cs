using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelFormatter<TEnvelope>
    : INotificationChannelFormatter
    where TEnvelope : class, INotificationEnvelope
{
    public abstract string ChannelKey { get; }

    public INotificationEnvelope Format(Notification notification)
        => FormatTyped(notification);

    protected abstract TEnvelope FormatTyped(Notification notification);
}
