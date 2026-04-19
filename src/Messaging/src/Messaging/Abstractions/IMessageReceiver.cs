namespace EtherGizmos.Common.Abstractions;

public interface IMessageReceiver
{
    IServiceProvider Services { get; }

    Task ReceiveAsync(ReceivedMessage message, CancellationToken cancellationToken = default);
}
