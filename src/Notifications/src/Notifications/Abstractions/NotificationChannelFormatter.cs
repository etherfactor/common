using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelFormatter<TMode, TMethod, TEnvelope, TModel>
    : INotificationChannelFormatter<TMode, TMethod, TModel>
    where TMode : DeliveryMode
    where TMethod : DeliveryMethod
    where TEnvelope : class, INotificationEnvelope<TMethod>
    where TModel : class
{
    public abstract TEnvelope Format(
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
