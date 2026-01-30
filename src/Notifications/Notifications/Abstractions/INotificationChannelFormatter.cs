using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelFormatter<TMode, TMethod, TModel>
    where TMode : DeliveryMode
    where TMethod : DeliveryMethod
    where TModel : class
{
    INotificationEnvelope Format(
        Notification notification,
        TModel model);
}
