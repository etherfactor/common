using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelFormatter<TMode, TMethod, TModel, TEnvelope>
    : INotificationChannelFormatter<TMode, TMethod, TModel>
    where TMode : DeliveryMode
    where TMethod : DeliveryMethod
    where TModel : class
    where TEnvelope : class, INotificationEnvelope<TMethod>
{
    public abstract string ChannelKey { get; }

    public INotificationEnvelope<TMethod> Format(
        Notification notification,
        TModel model)
        => FormatTyped(notification, model);

    protected abstract TEnvelope FormatTyped(
        Notification notification,
        TModel model);
}
