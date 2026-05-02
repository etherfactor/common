using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelFormatter<TSchedule, TChannel, TEnvelope, TModel>
    : INotificationChannelFormatter<TSchedule, TChannel, TModel>
    where TSchedule : NotificationSchedule
    where TChannel : NotificationChannel
    where TEnvelope : class, INotificationEnvelope<TChannel>
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
