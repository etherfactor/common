namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelSender<TMethod>
    where TMethod : DeliveryMethod
{
    Task SendAsync(
        INotificationEnvelope envelope,
        CancellationToken cancellationToken = default);
}
