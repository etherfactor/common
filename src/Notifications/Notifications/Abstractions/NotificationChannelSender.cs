namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelSender<TMethod, TEnvelope> : INotificationChannelSender<TMethod>
    where TMethod : DeliveryMethod
    where TEnvelope : class, INotificationEnvelope<TMethod>
{
    public async Task SendAsync(
        INotificationEnvelope<TMethod> envelope,
        CancellationToken cancellationToken = default)
    {
        if (envelope is not TEnvelope typed)
        {
            throw new InvalidOperationException(
                $"Envelope type mismatch for channel '{typeof(TMethod)}'. " +
                $"Expected '{typeof(TEnvelope).Name}', got '{envelope.GetType().Name}'.");
        }

        await SendTypedAsync(typed, cancellationToken);
    }

    protected abstract Task SendTypedAsync(
        TEnvelope envelope,
        CancellationToken cancellationToken = default);
}
