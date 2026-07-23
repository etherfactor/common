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
    where TSchedule : NotificationScheduleRef
    where TChannel : NotificationChannelRef
    where TModel : class;
