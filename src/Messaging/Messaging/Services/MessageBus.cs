using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace EtherGizmos.Common.Services;

internal class MessageBus : IMessageBus
{
    private readonly ILogger _logger;
    private readonly IMessageListenerFactory _listenerFactory;
    private readonly IMessagePublisherFactory _publisherFactory;
    private readonly IMessageReceiver _receiver;

    private readonly ConcurrentDictionary<string, Lazy<Task<(IMessageListener Listener, CancellationTokenSource Cts)>>> _listeners = [];
    private readonly ConcurrentDictionary<string, Lazy<Task<IMessagePublisher>>> _publishers = [];

    private ActionBlock<ReceivedMessage>? _pump;
    private CancellationTokenSource? _pumpCts;

    public MessageBus(
        ILogger<MessageBus> logger,
        IMessageListenerFactory listenerFactory,
        IMessagePublisherFactory publisherFactory,
        IMessageReceiver receiver)
    {
        _logger = logger;
        _listenerFactory = listenerFactory;
        _publisherFactory = publisherFactory;
        _receiver = receiver;
    }

    public bool TryGetListener(
        string logicalName, [NotNullWhen(true)] out IMessageListener? listener)
    {
        listener = null;

        if (_listeners.TryGetValue(logicalName, out var lazy))
        {
            try
            {
                var tuple = lazy.Value.GetAwaiter().GetResult();
                listener = tuple.Listener;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TryGetListener faulted for {LogicalName}. Removing lazy to allow retry.", logicalName);
                _listeners.TryRemove(logicalName, out _);
            }
        }

        return false;
    }

    public bool TryGetPublisher(
        string logicalName, [NotNullWhen(true)] out IMessagePublisher? publisher)
    {
        publisher = null;

        if (_publishers.TryGetValue(logicalName, out var lazy))
        {
            try
            {
                publisher = lazy.Value.GetAwaiter().GetResult();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "TryGetPublisher faulted for {LogicalName}. Removing lazy to allow retry.", logicalName);
                _publishers.TryRemove(logicalName, out _);
            }
        }

        return false;
    }

    public async Task<IMessageListener> RegisterListenerForQueueAsync(
        string logicalName, string queue, CancellationToken cancellationToken = default)
    {
        var lazy = new Lazy<Task<(IMessageListener Listener, CancellationTokenSource Cts)>>(async () =>
        {
            var listener = _listenerFactory.CreateListenerForQueue(logicalName, queue);
            await listener.StartAsync(cancellationToken).ConfigureAwait(false);

            var cts = new CancellationTokenSource();
            _ = ExecuteListenerPumpAsync(logicalName, listener, cts.Token);

            return (listener, cts);
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        if (!_listeners.TryAdd(logicalName, lazy))
            throw new InvalidOperationException("Listener already registered");

        var result = await lazy.Value.ConfigureAwait(false);
        return result.Listener;
    }

    public async Task<IMessageListener> RegisterListenerForTopicAsync(
        string logicalName, string topic, string subscription, CancellationToken cancellationToken = default)
    {
        var lazy = new Lazy<Task<(IMessageListener Listener, CancellationTokenSource Cts)>>(async () =>
        {
            var listener = _listenerFactory.CreateListenerForTopic(logicalName, topic, subscription);
            await listener.StartAsync(cancellationToken).ConfigureAwait(false);

            var cts = new CancellationTokenSource();
            _ = ExecuteListenerPumpAsync(logicalName, listener, cts.Token);

            return (listener, cts);
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        if (!_listeners.TryAdd(logicalName, lazy))
            throw new InvalidOperationException("Listener already registered");

        var result = await lazy.Value.ConfigureAwait(false);
        return result.Listener;
    }

    public async Task<IMessagePublisher> RegisterPublisherForQueueAsync(
        string logicalName, string queue, CancellationToken cancellationToken = default)
    {
        var lazy = new Lazy<Task<IMessagePublisher>>(async () =>
        {
            var publisher = _publisherFactory.CreatePublisherForQueue(logicalName, queue);
            await publisher.StartAsync(cancellationToken).ConfigureAwait(false);
            return publisher;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        if (!_publishers.TryAdd(logicalName, lazy))
            throw new InvalidOperationException("Publisher already registered");

        return await lazy.Value.ConfigureAwait(false);
    }

    public async Task<IMessagePublisher> RegisterPublisherForTopicAsync(
        string logicalName, string topic, CancellationToken cancellationToken = default)
    {
        var lazy = new Lazy<Task<IMessagePublisher>>(async () =>
        {
            var publisher = _publisherFactory.CreatePublisherForTopic(logicalName, topic);
            await publisher.StartAsync(cancellationToken).ConfigureAwait(false);
            return publisher;
        }, LazyThreadSafetyMode.ExecutionAndPublication);

        if (!_publishers.TryAdd(logicalName, lazy))
            throw new InvalidOperationException("Publisher already registered");

        return await lazy.Value.ConfigureAwait(false);
    }

    public Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        var parallelism = 8;
        _pumpCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        _pump = new ActionBlock<ReceivedMessage>(
            async message =>
            {
                try
                {
                    await _receiver.ReceiveAsync(message, _pumpCts.Token).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Receiver error; abandoning message.");
                    try
                    {
                        if (!message.Actions.Invoked)
                            await message.Actions.AbandonAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex2)
                    {
                        _logger.LogError(ex2, "Failed to abandon message.");
                    }
                }
            },
            new()
            {
                MaxDegreeOfParallelism = parallelism,
                BoundedCapacity = parallelism,
                CancellationToken = _pumpCts.Token,
            });

        _logger.LogInformation("MessageBus pump started with parallelism {Parallelism}.", parallelism);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping MessageBus...");

        // 1) Cancel listener pumps first so they stop pushing into the bus
        foreach (var key in _listeners.Keys.ToList())
        {
            if (_listeners.TryGetValue(key, out var lazy))
            {
                try
                {
                    var tuple = await lazy.Value.ConfigureAwait(false);
                    tuple.Cts.Cancel();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to cancel pump CTS for {LogicalName}.", key);
                }
            }
        }

        // 2) Stop the main pump
        _pumpCts?.Cancel();
        _pump?.Complete();
        if (_pump is not null)
        {
            try { await _pump.Completion.ConfigureAwait(false); }
            catch (Exception ex) { _logger.LogWarning(ex, "Pump completion observed fault."); }
        }
        _pump = null;
        _pumpCts?.Dispose();
        _pumpCts = null;

        // 3) Stop publishers, then listeners
        foreach (var logicalName in _publishers.Keys.ToList())
            await UnregisterPublisherAsync(logicalName, cancellationToken).ConfigureAwait(false);

        foreach (var logicalName in _listeners.Keys.ToList())
            await UnregisterListenerAsync(logicalName, cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("MessageBus stopped.");
    }

    public async Task UnregisterListenerAsync(string logicalName, CancellationToken cancellationToken = default)
    {
        if (_listeners.Remove(logicalName, out var lazy))
        {
            try
            {
                var (listener, cts) = await lazy.Value.ConfigureAwait(false);
                cts.Cancel();
                cts.Dispose();
                await listener.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop listener {LogicalName}.", logicalName);
            }
        }
    }

    public async Task UnregisterPublisherAsync(string logicalName, CancellationToken cancellationToken = default)
    {
        if (_publishers.Remove(logicalName, out var lazy))
        {
            try
            {
                var publisher = await lazy.Value.ConfigureAwait(false);
                await publisher.StopAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to stop publisher {LogicalName}.", logicalName);
            }
        }
    }

    private async Task ExecuteListenerPumpAsync(
        string logicalName,
        IMessageListener listener,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Pump loop starting for {LogicalName}.", logicalName);

        try
        {
            await foreach (var message in listener.Channel.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                var pump = _pump;
                if (pump is null)
                {
                    _logger.LogWarning("Pump is null; dropping message for {LogicalName}.", logicalName);
                    continue;
                }

                if (pump.Completion.IsCompleted)
                {
                    _logger.LogWarning("Pump completed; dropping message for {LogicalName}.", logicalName);
                    continue;
                }

                var accepted = await pump.SendAsync(message, cancellationToken).ConfigureAwait(false);
                if (!accepted)
                {
                    _logger.LogWarning("Pump rejected message for {LogicalName}.", logicalName);
                }
            }

            _logger.LogWarning("Channel completed; terminating pump for {LogicalName}.", logicalName);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation("Pump loop canceled for {LogicalName}.", logicalName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in pump loop for {LogicalName}.", logicalName);
        }
    }
}
