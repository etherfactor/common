namespace EtherGizmos.Common.Abstractions;

public interface IEmailNotificationChannelFormatter<TMode, TModel>
    : INotificationChannelFormatter<TMode, EmailChannel, TModel>
    where TMode : NotificationSchedule
    where TModel : class;
