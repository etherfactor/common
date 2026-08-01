using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Channels;

namespace EtherGizmos.Common.Services;

internal sealed class RabbitMQListener : IMessageListenerTransport, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly string? _queue;
    private readonly string? _topic;
    private readonly string? _subscription;
    private readonly object _deliverySync = new();
    private readonly SemaphoreSlim _stopGate = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private AsyncEventingBasicConsumer? _consumer;
    private ChannelWriter<ReceivedMessage>? _output;
    private string? _consumerTag;
    private TaskCompletionSource? _deliveriesDrained;
    private int _activeDeliveries;
    private volatile bool _stopping;
    private bool _stopped;

    public RabbitMQListener(
        ILogger<RabbitMQListener> logger,
        ConnectionFactory connectionFactory,
        string queue)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _queue = queue;
    }

    public RabbitMQListener(
        ILogger<RabbitMQListener> logger,
        ConnectionFactory connectionFactory,
        string topic,
        string subscription)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _topic = topic;
        _subscription = subscription;
    }

    public async Task StartAsync(
        ChannelWriter<ReceivedMessage> output,
        CancellationToken cancellationToken = default)
    {
        _output = output;
        _stopping = false;

        try
        {
            _connection = await _connectionFactory
                .CreateConnectionAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _connection.CallbackExceptionAsync += OnConnectionCallbackExceptionAsync;
            _connection.ConnectionShutdownAsync += OnConnectionShutdownAsync;

            _channel = await _connection
                .CreateChannelAsync(cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            _channel.ChannelShutdownAsync += OnChannelShutdownAsync;

            var queueName = await ConfigureTopologyAsync(
                _channel,
                cancellationToken).ConfigureAwait(false);

            await _channel.BasicQosAsync(
                prefetchSize: 0,
                prefetchCount: 50,
                global: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _consumer = new AsyncEventingBasicConsumer(_channel);
            _consumer.ReceivedAsync += OnReceivedAsync;
            _consumer.ShutdownAsync += OnConsumerShutdownAsync;
            _consumer.UnregisteredAsync += OnConsumerUnregisteredAsync;
            _consumer.RegisteredAsync += OnConsumerRegisteredAsync;

            _consumerTag = await _channel.BasicConsumeAsync(
                queue: queueName,
                autoAck: false,
                consumer: _consumer,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            _logger.LogInformation(
                "RabbitMQ listener started for {QueueOrTopic}.",
                DisplayName);
        }
        catch
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task StopReceivingAsync(
        CancellationToken cancellationToken = default)
    {
        if (_stopping)
        {
            await WaitForDeliveriesAsync(cancellationToken).ConfigureAwait(false);
            return;
        }

        _stopping = true;

        var channel = _channel;
        var consumerTag = _consumerTag;

        if (channel is not null &&
            !string.IsNullOrWhiteSpace(consumerTag) &&
            channel.IsOpen)
        {
            try
            {
                await channel.BasicCancelAsync(
                    consumerTag,
                    noWait: false,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (AlreadyClosedException)
            {
                // The broker/channel already stopped delivery.
            }
        }

        if (_consumer is not null)
            _consumer.ReceivedAsync -= OnReceivedAsync;

        _consumerTag = null;
        await WaitForDeliveriesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        await _stopGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_stopped)
                return;

            await StopReceivingAsync(cancellationToken).ConfigureAwait(false);
            DetachConsumerEvents();

            if (_channel is not null)
            {
                _channel.ChannelShutdownAsync -= OnChannelShutdownAsync;

                try
                {
                    await _channel.DisposeAsync()
                        .AsTask()
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing RabbitMQ listener channel.");
                }

                _channel = null;
            }

            if (_connection is not null)
            {
                _connection.CallbackExceptionAsync -= OnConnectionCallbackExceptionAsync;
                _connection.ConnectionShutdownAsync -= OnConnectionShutdownAsync;

                try
                {
                    await _connection.DisposeAsync()
                        .AsTask()
                        .WaitAsync(cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing RabbitMQ listener connection.");
                }

                _connection = null;
            }

            _consumer = null;
            _output = null;
            _stopped = true;

            _logger.LogInformation(
                "RabbitMQ listener stopped for {QueueOrTopic}.",
                DisplayName);
        }
        finally
        {
            _stopGate.Release();
        }
    }

    private async Task<string> ConfigureTopologyAsync(
        IChannel channel,
        CancellationToken cancellationToken)
    {
        if (_queue is not null)
        {
            await channel.QueueDeclareAsync(
                queue: _queue,
                durable: true,
                exclusive: false,
                autoDelete: false,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            return _queue;
        }

        await channel.ExchangeDeclareAsync(
            exchange: _topic!,
            type: ExchangeType.Fanout,
            durable: true,
            autoDelete: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var queueName = $"{_topic}:{_subscription}";

        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: _topic!,
            routingKey: string.Empty,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return queueName;
    }

    private async Task OnReceivedAsync(
        object sender,
        BasicDeliverEventArgs @event)
    {
        EnterDelivery();
        try
        {
            if (_stopping || @event.CancellationToken.IsCancellationRequested)
                return;

            var channel = _channel;
            var output = _output;
            if (channel is null || output is null)
                return;

            try
            {
                var body = Encoding.UTF8.GetString(@event.Body.Span);
                var rawHeaders = @event.BasicProperties.Headers
                    ?? new Dictionary<string, object?>();

                var allHeaders = rawHeaders.ToDictionary(
                    pair => pair.Key,
                    pair => AsString(pair.Value));

                if (!allHeaders.TryGetValue("$type", out var typeHeader) ||
                    string.IsNullOrWhiteSpace(typeHeader))
                {
                    _logger.LogWarning(
                        "Received message without $type header. DeliveryTag={DeliveryTag}",
                        @event.DeliveryTag);
                    typeHeader = string.Empty;
                }

                if (!allHeaders.TryGetValue("$logical", out var logicalHeader) ||
                    string.IsNullOrWhiteSpace(logicalHeader))
                {
                    _logger.LogWarning(
                        "Received message without $logical header. DeliveryTag={DeliveryTag}",
                        @event.DeliveryTag);
                    logicalHeader = string.Empty;
                }

                var headers = allHeaders
                    .Where(pair => pair.Key is not "$type" and not "$logical")
                    .ToImmutableDictionary();

                var actions = new RabbitMQMessageActions(
                    _logger,
                    channel,
                    @event.DeliveryTag,
                    @event.Redelivered);

                var subscription = _subscription is not null
                    ? $"{logicalHeader}/{_subscription}"
                    : logicalHeader;

                var message = new ReceivedMessage
                {
                    MessageId = @event.BasicProperties.MessageId
                        ?? @event.DeliveryTag.ToString(),
                    Type = typeHeader,
                    Body = body,
                    Headers = headers,
                    LogicalSourceName = logicalHeader,
                    SubscriptionName = subscription,
                    Actions = actions,
                };

                await output
                    .WriteAsync(message, @event.CancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException)
                when (_stopping || @event.CancellationToken.IsCancellationRequested)
            {
            }
            catch (ChannelClosedException)
                when (_stopping)
            {
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while receiving RabbitMQ message {DeliveryTag}.",
                    @event.DeliveryTag);

                if (!_stopping &&
                    channel.IsOpen &&
                    !@event.CancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await channel.BasicNackAsync(
                            deliveryTag: @event.DeliveryTag,
                            multiple: false,
                            requeue: !@event.Redelivered,
                            cancellationToken: @event.CancellationToken)
                            .ConfigureAwait(false);
                    }
                    catch (Exception nackException)
                    {
                        _logger.LogError(
                            nackException,
                            "Failed to nack RabbitMQ message {DeliveryTag}.",
                            @event.DeliveryTag);
                    }
                }
            }
        }
        finally
        {
            ExitDelivery();
        }
    }

    private void EnterDelivery()
    {
        lock (_deliverySync)
        {
            _activeDeliveries++;
        }
    }

    private void ExitDelivery()
    {
        TaskCompletionSource? drained = null;

        lock (_deliverySync)
        {
            _activeDeliveries--;
            if (_activeDeliveries == 0 && _stopping)
                drained = _deliveriesDrained;
        }

        drained?.TrySetResult();
    }

    private Task WaitForDeliveriesAsync(CancellationToken cancellationToken)
    {
        Task waitTask;

        lock (_deliverySync)
        {
            if (_activeDeliveries == 0)
                return Task.CompletedTask;

            _deliveriesDrained ??= new TaskCompletionSource(
                TaskCreationOptions.RunContinuationsAsynchronously);
            waitTask = _deliveriesDrained.Task;
        }

        return waitTask.WaitAsync(cancellationToken);
    }

    private void DetachConsumerEvents()
    {
        if (_consumer is null)
            return;

        _consumer.ReceivedAsync -= OnReceivedAsync;
        _consumer.ShutdownAsync -= OnConsumerShutdownAsync;
        _consumer.UnregisteredAsync -= OnConsumerUnregisteredAsync;
        _consumer.RegisteredAsync -= OnConsumerRegisteredAsync;
    }

    private Task OnConnectionCallbackExceptionAsync(
        object sender,
        CallbackExceptionEventArgs args)
    {
        _logger.LogError(args.Exception, "RabbitMQ listener connection callback exception.");
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdownAsync(
        object sender,
        ShutdownEventArgs args)
    {
        _logger.LogWarning(
            "RabbitMQ listener connection shutdown: {ReplyText} ({ReplyCode}).",
            args.ReplyText,
            (int)args.ReplyCode);
        return Task.CompletedTask;
    }

    private Task OnChannelShutdownAsync(
        object sender,
        ShutdownEventArgs args)
    {
        if (args.Exception is not null)
        {
            _logger.LogError(
                args.Exception,
                "RabbitMQ listener channel shutdown: {ReplyText} ({ReplyCode}).",
                args.ReplyText,
                (int)args.ReplyCode);
        }
        else
        {
            _logger.LogWarning(
                "RabbitMQ listener channel shutdown: {ReplyText} ({ReplyCode}).",
                args.ReplyText,
                (int)args.ReplyCode);
        }

        return Task.CompletedTask;
    }

    private Task OnConsumerShutdownAsync(
        object sender,
        ShutdownEventArgs args)
    {
        _logger.LogWarning(
            "RabbitMQ consumer shutdown: {ReplyText} ({ReplyCode}).",
            args.ReplyText,
            (int)args.ReplyCode);
        return Task.CompletedTask;
    }

    private Task OnConsumerUnregisteredAsync(
        object sender,
        ConsumerEventArgs args)
    {
        _logger.LogInformation(
            "RabbitMQ consumer unregistered: {@ConsumerTags}.",
            args.ConsumerTags);
        return Task.CompletedTask;
    }

    private Task OnConsumerRegisteredAsync(
        object sender,
        ConsumerEventArgs args)
    {
        _logger.LogInformation(
            "RabbitMQ consumer registered: {@ConsumerTags}.",
            args.ConsumerTags);
        return Task.CompletedTask;
    }

    private static string AsString(object? value)
        => value switch
        {
            null => string.Empty,
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            ReadOnlyMemory<byte> memory => Encoding.UTF8.GetString(memory.Span),
            string text => text,
            _ => value.ToString() ?? string.Empty,
        };

    private string DisplayName
        => _queue ?? $"{_topic}:{_subscription}";

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _stopGate.Dispose();
    }
}
