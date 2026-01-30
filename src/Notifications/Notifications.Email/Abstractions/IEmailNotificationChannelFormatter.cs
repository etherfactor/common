namespace EtherGizmos.Common.Abstractions;

public interface IEmailNotificationChannelFormatter<TMode, TModel>
    : INotificationChannelFormatter<TMode, EmailMethod, TModel>
    where TMode : DeliveryMode
    where TModel : class;
