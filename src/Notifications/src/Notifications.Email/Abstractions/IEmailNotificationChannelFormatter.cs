namespace EtherGizmos.Common.Abstractions;

public interface IEmailNotificationChannelFormatter<TMode, TModel>
    : INotificationChannelFormatter<TMode, EmailChannel, TModel>
    where TMode : NotificationScheduleRef
    where TModel : class;
