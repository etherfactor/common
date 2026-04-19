namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelSender<TMethod, TEnvelope> : INotificationChannelSender<TMethod>
    where TMethod : DeliveryMethod
    where TEnvelope : class, INotificationEnvelope<TMethod>
{
    public abstract Task SendAsync(
        TEnvelope envelope,
        CancellationToken cancellationToken = default);

    public Task SendAsync(
        INotificationEnvelope envelope,
        CancellationToken cancellationToken = default)
        => SendAsync(TryCast(envelope), cancellationToken);

    protected TEnvelope TryCast(
        INotificationEnvelope envelope)
    {
        if (envelope is not TEnvelope typed)
            throw new InvalidOperationException($"Must provide an envelope of type {typeof(INotificationEnvelope<TMethod>)}");

        return typed;
    }
}
