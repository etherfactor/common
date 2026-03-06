using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelFormatter
{
    INotificationEnvelope Format(
        Notification notification,
        object model);
}

public interface INotificationChannelFormatter<TMode, TMethod, TModel>
    : INotificationChannelFormatter
    where TMode : DeliveryMode
    where TMethod : DeliveryMethod
    where TModel : class
{
    INotificationEnvelope<TMethod> Format(
        Notification notification,
        TModel model);
}
