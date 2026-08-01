using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks.Dataflow;

namespace EtherGizmos.Common.Services;

internal sealed class MessageBus : IMessageBus, IAsyncDisposable
{
    private const int ReceiverParallelism = 8;
    private const int ListenerBufferCapacity = 50;
    private const int PublisherBufferCapacity = 100;
    private static readonly TimeSpan ForcedCleanupTimeout = TimeSpan.FromSeconds(5);

    private readonly BusKey _busKey;
    private readonly ILogger _logger;
    private readonly IMessageBusRegistry _registry;
    private readonly IMessageListenerFactory _listenerFactory;
    private readonly IMessagePublisherFactory _publisherFactory;
    private readonly IMessageReceiver _receiver;

    private readonly ConcurrentDictionary<string, ListenerEndpointRuntime> _listeners = [];
    private readonly ConcurrentDictionary<string, PublisherEndpointRuntime> _publishers = [];
    private readonly SemaphoreSlim _lifecycleGate = new(1, 1);

    private CancellationTokenSource? _abortCts;
    private ActionBlock<ReceivedMessageWorkItem>? _receiverPump;
    private int _state = (int)MessageBusLifecycleState.Created;

    public MessageBus(
        [ServiceKey] object serviceKey,
        ILogger<MessageBus> logger,
        IServiceProvider serviceProvider,
        IMessageBusRegistry registry,
        IMessageReceiver receiver)
    {
        _busKey = (BusKey)serviceKey;
        _logger = logger;
        _registry = registry;
        _listenerFactory = serviceProvider.GetRequiredKeyedService<IMessageListenerFactory>(_busKey);
        _publisherFactory = serviceProvider.GetRequiredKeyedService<IMessagePublisherFactory>(_busKey);
        _receiver = receiver;
    }

    public bool TryGetListener(
        string logicalName,
        [NotNullWhen(true)] out IMessageListener? listener)
    {
        if (_listeners.TryGetValue(logicalName, out var runtime))
        {
            listener = runtime;
            return true;
        }

        listener = null;
        return false;
    }

    public bool TryGetPublisher(
        string logicalName,
        [NotNullWhen(true)] out IMessagePublisher? publisher)
    {
        if (_publishers.TryGetValue(logicalName, out var runtime))
        {
            publisher = runtime;
            return true;
        }

        publisher = null;
        return false;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = State;
            if (state != MessageBusLifecycleState.Created)
                throw new InvalidOperationException($"Message bus '{_busKey.BusId}' cannot start from state '{state}'.");

            _abortCts = new CancellationTokenSource();
            _receiverPump = CreateReceiverPump(_abortCts.Token);
            SetState(MessageBusLifecycleState.Running);

            _logger.LogInformation(
                "MessageBus {BusId} started with receiver parallelism {Parallelism}.",
                _busKey.BusId,
                ReceiverParallelism);
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    public Task<IMessageListener> RegisterListenerForQueueAsync(
        string logicalName,
        string queue,
        CancellationToken cancellationToken = default)
        => RegisterListenerAsync(
            logicalName,
            () => _listenerFactory.CreateListenerForQueue(logicalName, queue),
            cancellationToken);

    public Task<IMessageListener> RegisterListenerForTopicAsync(
        string logicalName,
        string topic,
        string subscription,
        CancellationToken cancellationToken = default)
        => RegisterListenerAsync(
            logicalName,
            () => _listenerFactory.CreateListenerForTopic(logicalName, topic, subscription),
            cancellationToken);

    public Task<IMessagePublisher> RegisterPublisherForQueueAsync(
        string logicalName,
        string queue,
        CancellationToken cancellationToken = default)
        => RegisterPublisherAsync(
            logicalName,
            () => _publisherFactory.CreatePublisherForQueue(logicalName, queue),
            cancellationToken);

    public Task<IMessagePublisher> RegisterPublisherForTopicAsync(
        string logicalName,
        string topic,
        CancellationToken cancellationToken = default)
        => RegisterPublisherAsync(
            logicalName,
            () => _publisherFactory.CreatePublisherForTopic(logicalName, topic),
            cancellationToken);

    public async Task UnregisterListenerAsync(
        string logicalName,
        CancellationToken cancellationToken = default)
    {
        if (!_listeners.TryRemove(logicalName, out var runtime))
            return;

        try
        {
            await runtime.StopReceivingAsync(cancellationToken).ConfigureAwait(false);
            await runtime.WaitForDrainAsync(cancellationToken).ConfigureAwait(false);
            await runtime.StopTransportAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            runtime.Abort();
            throw;
        }
        finally
        {
            _registry.UnregisterListener(logicalName);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task UnregisterPublisherAsync(
        string logicalName,
        CancellationToken cancellationToken = default)
    {
        if (!_publishers.TryRemove(logicalName, out var runtime))
            return;

        try
        {
            runtime.StopAccepting();
            await runtime.WaitForDrainAsync(cancellationToken).ConfigureAwait(false);
            await runtime.StopTransportAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            runtime.Abort();
            throw;
        }
        finally
        {
            _registry.UnregisterPublisher(logicalName);
            await runtime.DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _lifecycleGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (State == MessageBusLifecycleState.Stopped)
                return;

            if (State != MessageBusLifecycleState.Running)
                throw new InvalidOperationException($"Message bus '{_busKey.BusId}' cannot stop from state '{State}'.");

            SetState(MessageBusLifecycleState.Draining);
            _logger.LogInformation("Draining MessageBus {BusId}...", _busKey.BusId);

            try
            {
                await DrainAsync(cancellationToken).ConfigureAwait(false);
                SetState(MessageBusLifecycleState.Stopping);
                await StopTransportsAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning(
                    "Graceful shutdown timed out for MessageBus {BusId}; aborting remaining work.",
                    _busKey.BusId);

                SetState(MessageBusLifecycleState.Stopping);
                Abort();
                await ForceCleanupAsync().ConfigureAwait(false);
                throw;
            }
            catch
            {
                SetState(MessageBusLifecycleState.Stopping);
                Abort();
                await ForceCleanupAsync().ConfigureAwait(false);
                throw;
            }
            finally
            {
                await DisposeRuntimesAsync().ConfigureAwait(false);
                RemoveRegistryMappings();
                _listeners.Clear();
                _publishers.Clear();
                _receiverPump = null;

                _abortCts?.Dispose();
                _abortCts = null;

                SetState(MessageBusLifecycleState.Stopped);
                _logger.LogInformation("MessageBus {BusId} stopped.", _busKey.BusId);
            }
        }
        finally
        {
            _lifecycleGate.Release();
        }
    }

    private async Task<IMessageListener> RegisterListenerAsync(
        string logicalName,
        Func<IMessageListenerTransport> createTransport,
        CancellationToken cancellationToken)
    {
        EnsureRunning();

        var runtime = new ListenerEndpointRuntime(
            _logger,
            logicalName,
            createTransport(),
            ForwardToReceiverPumpAsync,
            ListenerBufferCapacity);

        if (!_listeners.TryAdd(logicalName, runtime))
        {
            await runtime.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"The listener '{logicalName}' is already registered.");
        }

        try
        {
            _registry.RegisterListener(_busKey.BusId, logicalName);
            await runtime.StartAsync(cancellationToken).ConfigureAwait(false);
            return runtime;
        }
        catch
        {
            _listeners.TryRemove(logicalName, out _);
            _registry.UnregisterListener(logicalName);
            try
            {
                await runtime.StopTransportAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up listener {LogicalName} after startup failure.", logicalName);
            }
            await runtime.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private async Task<IMessagePublisher> RegisterPublisherAsync(
        string logicalName,
        Func<IMessagePublisherTransport> createTransport,
        CancellationToken cancellationToken)
    {
        EnsureRunning();

        var runtime = new PublisherEndpointRuntime(
            _logger,
            logicalName,
            createTransport(),
            PublisherBufferCapacity);

        if (!_publishers.TryAdd(logicalName, runtime))
        {
            await runtime.DisposeAsync().ConfigureAwait(false);
            throw new InvalidOperationException($"The publisher '{logicalName}' is already registered.");
        }

        try
        {
            _registry.RegisterPublisher(_busKey.BusId, logicalName);
            await runtime.StartAsync(cancellationToken).ConfigureAwait(false);
            return runtime;
        }
        catch
        {
            _publishers.TryRemove(logicalName, out _);
            _registry.UnregisterPublisher(logicalName);
            try
            {
                await runtime.StopTransportAsync(CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clean up publisher {LogicalName} after startup failure.", logicalName);
            }
            await runtime.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    private ActionBlock<ReceivedMessageWorkItem> CreateReceiverPump(
        CancellationToken abortToken)
        => new(
            ProcessReceivedWorkItemAsync,
            new ExecutionDataflowBlockOptions
            {
                MaxDegreeOfParallelism = ReceiverParallelism,
                BoundedCapacity = ReceiverParallelism,
                CancellationToken = abortToken,
            });

    private async Task ProcessReceivedWorkItemAsync(ReceivedMessageWorkItem workItem)
    {
        var message = workItem.Message;
        var abortToken = _abortCts?.Token ?? CancellationToken.None;

        try
        {
            await _receiver
                .ReceiveAsync(message, abortToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (abortToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Message {MessageId} was canceled during forced shutdown.",
                message.MessageId);

            await TryAbandonAsync(message).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Receiver error for message {MessageId}; abandoning message.",
                message.MessageId);

            await TryAbandonAsync(message).ConfigureAwait(false);
        }
        finally
        {
            workItem.Complete();
        }
    }

    private async Task<MessageDispatchHandle> ForwardToReceiverPumpAsync(
        ReceivedMessage message,
        CancellationToken cancellationToken)
    {
        var pump = _receiverPump;
        if (pump is null)
            return new MessageDispatchHandle(false, Task.CompletedTask);

        var workItem = new ReceivedMessageWorkItem(message);
        var accepted = await pump
            .SendAsync(workItem, cancellationToken)
            .ConfigureAwait(false);

        return new MessageDispatchHandle(
            accepted,
            accepted ? workItem.Completion : Task.CompletedTask);
    }

    private async Task DrainAsync(CancellationToken cancellationToken)
    {
        // Freeze the outbound work set first. Any new SendAsync call is rejected.
        foreach (var publisher in _publishers.Values)
            publisher.StopAccepting();

        // Stop new RabbitMQ deliveries, but keep listener channels/connections alive
        // so messages already accepted by the bus can still be acknowledged.
        await Task.WhenAll(
                _listeners.Values.Select(listener =>
                    listener.StopReceivingAsync(cancellationToken)))
            .ConfigureAwait(false);

        // Drain transport-to-bus channels into the central receiver pump.
        await Task.WhenAll(
                _listeners.Values.Select(listener =>
                    listener.WaitForDrainAsync(cancellationToken)))
            .ConfigureAwait(false);

        // No listener can submit more work now, so the central pump has a finite set.
        _receiverPump?.Complete();
        if (_receiverPump is not null)
        {
            await _receiverPump.Completion
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        // Drain messages accepted before publishers entered the draining state.
        await Task.WhenAll(
                _publishers.Values.Select(publisher =>
                    publisher.WaitForDrainAsync(cancellationToken)))
            .ConfigureAwait(false);
    }

    private async Task StopTransportsAsync(CancellationToken cancellationToken)
    {
        await Task.WhenAll(
                _listeners.Values.Select(listener =>
                    listener.StopTransportAsync(cancellationToken)))
            .ConfigureAwait(false);

        await Task.WhenAll(
                _publishers.Values.Select(publisher =>
                    publisher.StopTransportAsync(cancellationToken)))
            .ConfigureAwait(false);
    }

    private void Abort()
    {
        _abortCts?.Cancel();

        foreach (var listener in _listeners.Values)
            listener.Abort();

        foreach (var publisher in _publishers.Values)
            publisher.Abort();

        _receiverPump?.Complete();
    }

    private async Task ForceCleanupAsync()
    {
        using var cleanupCts = new CancellationTokenSource(ForcedCleanupTimeout);

        var cleanupTasks = _listeners.Values
            .Select(listener => SafeStopTransportAsync(
                () => listener.StopTransportAsync(cleanupCts.Token),
                "listener",
                listener.LogicalName))
            .Concat(_publishers.Values.Select(publisher => SafeStopTransportAsync(
                () => publisher.StopTransportAsync(cleanupCts.Token),
                "publisher",
                publisher.LogicalName)));

        await Task.WhenAll(cleanupTasks).ConfigureAwait(false);
    }

    private async Task SafeStopTransportAsync(
        Func<Task> stopAsync,
        string endpointType,
        string logicalName)
    {
        try
        {
            await stopAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Forced cleanup failed for {EndpointType} {LogicalName}.",
                endpointType,
                logicalName);
        }
    }

    private async Task TryAbandonAsync(ReceivedMessage message)
    {
        if (message.Actions.Invoked)
            return;

        try
        {
            await message.Actions.AbandonAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to abandon message {MessageId}.",
                message.MessageId);
        }
    }


    private async Task DisposeRuntimesAsync()
    {
        foreach (var listener in _listeners.Values)
            await listener.DisposeAsync().ConfigureAwait(false);

        foreach (var publisher in _publishers.Values)
            await publisher.DisposeAsync().ConfigureAwait(false);
    }

    private void RemoveRegistryMappings()
    {
        foreach (var logicalName in _listeners.Keys)
            _registry.UnregisterListener(logicalName);

        foreach (var logicalName in _publishers.Keys)
            _registry.UnregisterPublisher(logicalName);
    }

    private void EnsureRunning()
    {
        if (State != MessageBusLifecycleState.Running)
            throw new InvalidOperationException($"Message bus '{_busKey.BusId}' is not running.");
    }

    private MessageBusLifecycleState State
        => (MessageBusLifecycleState)Volatile.Read(ref _state);

    private void SetState(MessageBusLifecycleState state)
        => Volatile.Write(ref _state, (int)state);

    public async ValueTask DisposeAsync()
    {
        if (State == MessageBusLifecycleState.Running)
        {
            using var cts = new CancellationTokenSource(ForcedCleanupTimeout);
            try
            {
                await StopAsync(cts.Token).ConfigureAwait(false);
            }
            catch
            {
                Abort();
            }
        }

        _abortCts?.Dispose();
        _lifecycleGate.Dispose();
    }
}
