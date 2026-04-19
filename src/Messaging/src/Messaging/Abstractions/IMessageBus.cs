using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common.Abstractions;

public interface IMessageBus
{
    Task StartAsync(CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);

    Task<IMessageListener> RegisterListenerForQueueAsync(
        string logicalName, string queue, CancellationToken cancellationToken = default);

    Task<IMessageListener> RegisterListenerForTopicAsync(
        string logicalName, string topic, string subscription, CancellationToken cancellationToken = default);

    Task<IMessagePublisher> RegisterPublisherForQueueAsync(
        string logicalName, string queue, CancellationToken cancellationToken = default);

    Task<IMessagePublisher> RegisterPublisherForTopicAsync(
        string logicalName, string topic, CancellationToken cancellationToken = default);

    Task UnregisterListenerAsync(string logicalName, CancellationToken cancellationToken = default);

    Task UnregisterPublisherAsync(string logicalName, CancellationToken cancellationToken = default);
    bool TryGetListener(string logicalName, [NotNullWhen(true)] out IMessageListener? listener);
    bool TryGetPublisher(string logicalName, [NotNullWhen(true)] out IMessagePublisher? publisher);
}
