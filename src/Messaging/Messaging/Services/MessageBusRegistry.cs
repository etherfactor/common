using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace EtherGizmos.Common.Services;

internal class MessageBusRegistry : IMessageBusRegistry
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, string> _buses = [];
    private readonly ConcurrentDictionary<string, Lazy<Task<(IMessageListener Listener, CancellationTokenSource Cts)>>> _listeners = [];
    private readonly ConcurrentDictionary<string, Lazy<Task<IMessagePublisher>>> _publishers = [];

    public MessageBusRegistry(
        ILogger<MessageBusRegistry> logger)
    {
        _logger = logger;
    }

    public bool TryGetBusId(
        string logicalName,
        [NotNullWhen(true)] out string? busId)
    {
        return _buses.TryGetValue(logicalName, out busId);
    }

    public async Task<IMessageListener> RegisterListenerAsync(
        string busId, string logicalName,
        Lazy<Task<(IMessageListener Listener, CancellationTokenSource Cts)>> listener, CancellationToken cancellationToken = default)
    {
        if (_buses.TryGetValue(logicalName, out var currentId) && currentId != busId)
            throw new InvalidOperationException($"The logical name {logicalName} cannot be registered to bus {busId} as it " +
                $"has already been registered to bus {currentId}");

        if (!_listeners.TryAdd(logicalName, listener))
            throw new InvalidOperationException($"The listener {logicalName} has already been registered");

        _buses.AddOrUpdate(logicalName, busId, (_, _) => busId);

        var result = await listener.Value.ConfigureAwait(false);
        return result.Listener;
    }

    public async Task<IMessagePublisher> RegisterPublisherAsync(
        string busId, string logicalName,
        Lazy<Task<IMessagePublisher>> publisher, CancellationToken cancellationToken = default)
    {
        if (_buses.TryGetValue(logicalName, out var currentId) && currentId != busId)
            throw new InvalidOperationException($"The logical name {logicalName} cannot be registered to bus {busId} as it " +
                $"has already been registered to bus {currentId}");

        if (!_publishers.TryAdd(logicalName, publisher))
            throw new InvalidOperationException($"The publisher {logicalName} has already been registered");

        _buses.AddOrUpdate(logicalName, busId, (_, _) => busId);

        var result = await publisher.Value.ConfigureAwait(false);
        return result;
    }

    public async Task UnregisterListenerAsync(
        string logicalName, CancellationToken cancellationToken = default)
    {
        if (_listeners.Remove(logicalName, out var lazy))
        {
            if (!_publishers.ContainsKey(logicalName))
                _buses.TryRemove(logicalName, out _);

            try
            {
                var (listener, cts) = await lazy.Value.ConfigureAwait(false);
                cts.Cancel();
                cts.Dispose();
                await listener.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop listener {LogicalName}", logicalName);
            }
        }
    }

    public async Task UnregisterPublisherAsync(
        string logicalName, CancellationToken cancellationToken = default)
    {
        if (_publishers.Remove(logicalName, out var lazy))
        {
            if (!_listeners.ContainsKey(logicalName))
                _buses.TryRemove(logicalName, out _);

            try
            {
                var publisher = await lazy.Value.ConfigureAwait(false);
                await publisher.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop publisher {LogicalName}", logicalName);
            }
        }
    }
}
