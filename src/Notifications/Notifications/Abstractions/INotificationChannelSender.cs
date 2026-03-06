namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelSender
{
    Task SendAsync(
        INotificationEnvelope envelope,
        CancellationToken cancellationToken = default);
}

public interface INotificationChannelSender<TMethod>
    : INotificationChannelSender
    where TMethod : DeliveryMethod
{
    Task SendAsync(
        INotificationEnvelope<TMethod> envelope,
        CancellationToken cancellationToken = default);
}
