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

    public abstract INotificationEnvelope<TMethod> Format(
        Notification notification,
        TModel model);

    public INotificationEnvelope Format(
        Notification notification,
        object model)
        => Format(notification, TryCast(model));

    private TModel TryCast(
        object model)
    {
        if (model is not TModel typed)
            throw new InvalidOperationException($"Must provide a model of type {typeof(TModel)}");

        return typed;
    }
}
