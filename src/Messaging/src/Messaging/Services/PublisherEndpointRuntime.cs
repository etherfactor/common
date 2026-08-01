using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using System.Threading.Channels;

namespace EtherGizmos.Common.Services;

internal sealed class PublisherEndpointRuntime : IMessagePublisher, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly IMessagePublisherTransport _transport;
    private readonly Channel<SentMessage> _channel;
    private readonly CancellationTokenSource _abortCts = new();

    private Task? _publishTask;
    private int _state;
    private int _started;
    private int _stopped;

    public string LogicalName { get; }

    public PublisherEndpointRuntime(
        ILogger logger,
        string logicalName,
        IMessagePublisherTransport transport,
        int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);

        _logger = logger;
        _transport = transport;
        LogicalName = logicalName;

        _channel = Channel.CreateBounded<SentMessage>(
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
            throw new InvalidOperationException($"Publisher '{LogicalName}' has already been started.");

        await _transport
            .StartAsync(cancellationToken)
            .ConfigureAwait(false);

        Volatile.Write(ref _state, 1); // Running
        _publishTask = PublishLoopAsync(_abortCts.Token);
    }

    public async Task SendAsync(
        SentMessage message,
        CancellationToken cancellationToken = default)
    {
        if (Volatile.Read(ref _state) != 1)
            throw new MessageBusStoppingException(LogicalName);

        try
        {
            await _channel.Writer
                .WriteAsync(message, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (ChannelClosedException)
        {
            throw new MessageBusStoppingException(LogicalName);
        }
    }

    public void StopAccepting()
    {
        var prior = Interlocked.CompareExchange(ref _state, 2, 1);
        if (prior == 1)
            _channel.Writer.TryComplete();
    }

    public async Task WaitForDrainAsync(CancellationToken cancellationToken)
    {
        if (_publishTask is null)
            return;

        await _publishTask
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task StopTransportAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _stopped, 1) != 0)
            return;

        Volatile.Write(ref _state, 3); // Stopped

        await _transport
            .StopAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public void Abort()
    {
        Volatile.Write(ref _state, 3);
        _channel.Writer.TryComplete();
        _abortCts.Cancel();
    }

    private async Task PublishLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Publisher loop starting for {LogicalName}.",
            LogicalName);

        try
        {
            await foreach (var message in _channel.Reader
                .ReadAllAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                await _transport
                    .PublishAsync(message, cancellationToken)
                    .ConfigureAwait(false);
            }

            _logger.LogInformation(
                "Publisher loop drained for {LogicalName}.",
                LogicalName);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "Publisher loop aborted for {LogicalName}.",
                LogicalName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Publisher loop failed for {LogicalName}.",
                LogicalName);
            throw;
        }
    }

    private async Task ObservePublishTaskAsync()
    {
        if (_publishTask is null)
            return;

        try
        {
            await _publishTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
            when (_abortCts.IsCancellationRequested)
        {
        }
    }

    public async ValueTask DisposeAsync()
    {
        Abort();
        await ObservePublishTaskAsync().ConfigureAwait(false);

        _abortCts.Dispose();
    }
}
