using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelFormatter<TMode, TMethod, TModel>
    where TMode : DeliveryMode
    where TMethod : DeliveryMethod
    where TModel : class
{
    INotificationEnvelope<TMethod> Format(
        Notification notification,
        TModel model);
}
