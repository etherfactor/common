namespace EtherGizmos.Common.Abstractions;

public interface INotificationEnvelope;

public interface INotificationEnvelope<TChannel>
    : INotificationEnvelope
    where TChannel : NotificationChannel;
