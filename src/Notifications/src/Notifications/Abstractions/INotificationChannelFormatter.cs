using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelFormatter
{
    INotificationEnvelope Format(
        Notification notification,
        object model);
}

public interface INotificationChannelFormatter<TSchedule, TChannel, TModel>
    : INotificationChannelFormatter
    where TSchedule : NotificationSchedule
    where TChannel : NotificationChannel
    where TModel : class;
