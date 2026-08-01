namespace EtherGizmos.Common.Abstractions;

public interface IMessagePublisher
{
    string LogicalName { get; }

    Task SendAsync(
        SentMessage message,
        CancellationToken cancellationToken = default);
}
