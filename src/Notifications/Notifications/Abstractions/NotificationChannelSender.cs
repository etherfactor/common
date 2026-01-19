namespace EtherGizmos.Common.Abstractions;

public abstract class NotificationChannelSender<TEnvelope> : INotificationChannelSender
    where TEnvelope : class, INotificationEnvelope
{
    public abstract string ChannelKey { get; }

    public async Task SendAsync(INotificationEnvelope envelope, CancellationToken ct)
    {
        if (envelope is not TEnvelope typed)
        {
            throw new InvalidOperationException(
                $"Envelope type mismatch for channel '{ChannelKey}'. " +
                $"Expected '{typeof(TEnvelope).Name}', got '{envelope.GetType().Name}'.");
        }

        await SendTypedAsync(typed, ct);
    }

    protected abstract Task SendTypedAsync(TEnvelope envelope, CancellationToken ct);
}
