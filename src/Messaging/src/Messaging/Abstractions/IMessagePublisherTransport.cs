namespace EtherGizmos.Common.Abstractions;

public interface IMessagePublisherTransport
{
    Task StartAsync(
        CancellationToken cancellationToken = default);

    Task PublishAsync(
        SentMessage message,
        CancellationToken cancellationToken = default);

    Task StopAsync(
        CancellationToken cancellationToken = default);
}
