
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common.Abstractions;

public interface IMessageBusRegistry
{
    Task<IMessageListener> RegisterListenerAsync(
        string busId,
        string logicalName,
        Lazy<Task<(IMessageListener Listener, CancellationTokenSource Cts)>> listener,
        CancellationToken cancellationToken = default);

    Task<IMessagePublisher> RegisterPublisherAsync(
        string busId,
        string logicalName,
        Lazy<Task<IMessagePublisher>> publisher,
        CancellationToken cancellationToken = default);

    bool TryGetBusId(
        string logicalName,
        [NotNullWhen(true)] out string? busId);
    
    Task UnregisterListenerAsync(
        string logicalName,
        CancellationToken cancellationToken = default);

    Task UnregisterPublisherAsync(
        string logicalName,
        CancellationToken cancellationToken = default);
}
