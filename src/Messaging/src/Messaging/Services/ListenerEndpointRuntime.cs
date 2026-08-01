using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EtherGizmos.Common.Services;

internal sealed class ListenerEndpointRuntime : IMessageListener, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IMessageListenerTransport _transport;
    private readonly Channel<ReceivedMessage> _channel;
    private readonly CancellationTokenSource _abortCts = new();
    private readonly Func<ReceivedMessage, CancellationToken, Task<MessageDispatchHandle>> _forwardAsync;
    private readonly TaskCompletionSource _processingDrained = new(
        TaskCreationOptions.RunContinuationsAsynchronously);
    private int _pendingProcessing;

    private Task? _pumpTask;
    private int _started;
    private int _quiesced;
    private int _stopped;

    public string LogicalName { get; }

    public ListenerEndpointRuntime(
        ILogger logger,
        string logicalName,
        IMessageListenerTransport transport,
        Func<ReceivedMessage, CancellationToken, Task<MessageDispatchHandle>> forwardAsync,
        int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _logger = logger;
        _transport = transport;
        _forwardAsync = forwardAsync;
        LogicalName = logicalName;

        _channel = Channel.CreateBounded<ReceivedMessage>(
            new BoundedChannelOptions(capacity)
            {
                SingleReader = true,
                SingleWriter = false,
                FullMode = BoundedChannelFullMode.Wait,
                AllowSynchronousContinuations = false,
            });
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
            throw new InvalidOperationException($"Listener '{LogicalName}' has already been started.");

        _pumpTask = PumpAsync(_abortCts.Token);

        try
        {
            await _transport
                .StartAsync(_channel.Writer, cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            Abort();
            await ObservePumpAsync().ConfigureAwait(false);
            throw;
        }
    }

    public async Task StopReceivingAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _quiesced, 1) != 0)
            return;

        await _transport
            .StopReceivingAsync(cancellationToken)
            .ConfigureAwait(false);

        // StopReceivingAsync guarantees no delivery callback can write again.
        _channel.Writer.TryComplete();
    }

    public async Task WaitForDrainAsync(CancellationToken cancellationToken)
    {
        if (_pumpTask is null)
            return;

        await _pumpTask
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        if (Volatile.Read(ref _pendingProcessing) > 0)
        {
            await _processingDrained.Task
                .WaitAsync(cancellationToken)
                .ConfigureAwait(false);
        }
    }

    public async Task StopTransportAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
            return;

        await _transport
            .StopAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Abort()
    {
        _channel.Writer.TryComplete();
        _abortCts.Cancel();
    }

    private async Task PumpAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Listener pump starting for {LogicalName}.",
            LogicalName);

        try
        {
            await foreach (var message in _channel.Reader
                .ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                var dispatch = await _forwardAsync(message, cancellationToken)
                    .ConfigureAwait(false);

                if (dispatch.Accepted)
                {
                    Interlocked.Increment(ref _pendingProcessing);
                    _ = ObserveProcessingAsync(dispatch.Completion);
                    continue;
                }

                if (!dispatch.Accepted)
                {
                    _logger.LogWarning(
                        "Message pump rejected a message for {LogicalName}.",
                        LogicalName);

                    if (!message.Actions.Invoked)
                    {
                        await message.Actions
                            .AbandonAsync(default)
                            .ConfigureAwait(false);
                    }
                }
            }

            _logger.LogInformation(
                "Listener pump drained for {LogicalName}.",
                LogicalName);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Listener pump aborted for {LogicalName}.",
                LogicalName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Listener pump failed for {LogicalName}.",
                LogicalName);
            throw;
        }
    }

    private async Task ObserveProcessingAsync(Task completion)
    {
        try
        {
            await completion.ConfigureAwait(false);
        }
        finally
        {
            if (Interlocked.Decrement(ref _pendingProcessing) == 0)
                _processingDrained.TrySetResult();
        }
    }

    private async Task ObservePumpAsync()
    {
        if (_pumpTask is null)
            return;

        try
        {
            await _pumpTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (_abortCts.IsCancellationRequested)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        Abort();
        await ObservePumpAsync().ConfigureAwait(false);

        _abortCts.Dispose();
    }
}
