namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelSender<TMethod> : INotificationChannelSender<TMethod>
    where TMethod : DeliveryMethod
{
    public abstract Task SendAsync(
        INotificationEnvelope<TMethod> envelope,
        CancellationToken cancellationToken = default);

    public Task SendAsync(
        INotificationEnvelope envelope,
        CancellationToken cancellationToken = default)
        => SendAsync(TryCast(envelope), cancellationToken);

    public INotificationEnvelope<TMethod> TryCast(
        INotificationEnvelope envelope)
    {
        if (envelope is not INotificationEnvelope<TMethod> typed)
            throw new InvalidOperationException($"Must provide an envelope of type {typeof(INotificationEnvelope<TMethod>)}");

        return typed;
    }
}
