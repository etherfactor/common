using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Immutable;
using System.Text;
using System.Threading.Channels;

namespace EtherGizmos.Common.Services;

internal class RabbitMQListener : IMessageListener, IDisposable
{
    private readonly ILogger _logger;
    private readonly ConnectionFactory _rmqConnectionFactory;
    private readonly string? _queue;
    private readonly string? _topic;
    private readonly string? _subscription;

    private readonly Channel<ReceivedMessage> _channel = System.Threading.Channels.Channel.CreateUnbounded<ReceivedMessage>();
    private volatile bool _stopping;

    private IConnection? _rmqConnection;
    private IChannel? _rmqChannel;
    private AsyncEventingBasicConsumer? _rmqConsumer;

    private bool _disposed;

    public ChannelReader<ReceivedMessage> Channel => _channel;

    public RabbitMQListener(
        ILogger<RabbitMQListener> logger,
        ConnectionFactory connectionFactory,
        string queue)
    {
        _logger = logger;
        _rmqConnectionFactory = connectionFactory;
        _queue = queue;
    }

    public RabbitMQListener(
        ILogger<RabbitMQListener> logger,
        ConnectionFactory connectionFactory,
        string topic,
        string subscription)
    {
        _logger = logger;
        _rmqConnectionFactory = connectionFactory;
        _topic = topic;
        _subscription = subscription;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _stopping = false;

        _rmqConnection = await _rmqConnectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        _rmqConnection.CallbackExceptionAsync += (_, e) =>
        {
            _logger.LogError(e.Exception, "RabbitMQ connection callback exception.");
            return Task.CompletedTask;
        };
        _rmqConnection.ConnectionShutdownAsync += (_, e) =>
        {
            _logger.LogWarning("RabbitMQ connection shutdown: {ReplyText} ({ReplyCode})", e.ReplyText, (int)e.ReplyCode);
            return Task.CompletedTask;
        };

        _rmqChannel = await _rmqConnection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        _rmqChannel.ChannelShutdownAsync += (_, e) =>
        {
            if (e.Exception is not null)
            {
                _logger.LogError(e.Exception, "RabbitMQ channel shutdown: {ReplyText} ({ReplyCode})", e.ReplyText, (int)e.ReplyCode);
            }
            else
            {
                _logger.LogWarning("RabbitMQ channel shutdown: {ReplyText} ({ReplyCode})", e.ReplyText, (int)e.ReplyCode);
            }
            return Task.CompletedTask;
        };

        if (_queue is not null)
        {
            await _rmqChannel.QueueDeclareAsync(_queue, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Keep fanout semantics; routing key is ignored on bind
            await _rmqChannel.ExchangeDeclareAsync(_topic!, ExchangeType.Fanout, durable: true, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            var queueName = $"{_topic}:{_subscription}";
            await _rmqChannel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: cancellationToken).ConfigureAwait(false);
            await _rmqChannel.QueueBindAsync(queueName, exchange: _topic!, routingKey: string.Empty, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // QoS for consumer (avoid unlimited unacked flood)
        await _rmqChannel.BasicQosAsync(0, prefetchCount: 50, global: false, cancellationToken).ConfigureAwait(false);

        _rmqConsumer = new AsyncEventingBasicConsumer(_rmqChannel);
        _rmqConsumer.ReceivedAsync += RmqConsumer_ReceivedAsync;
        _rmqConsumer.ShutdownAsync += (_, ea) =>
        {
            _logger.LogWarning("RabbitMQ consumer shutdown: {ReplyText} ({ReplyCode})", ea.ReplyText, (int)ea.ReplyCode);
            return Task.CompletedTask;
        };
        _rmqConsumer.UnregisteredAsync += (_, ea) =>
        {
            _logger.LogInformation("RabbitMQ consumer unregistered: {@ConsumerTags}", ea.ConsumerTags);
            return Task.CompletedTask;
        };
        _rmqConsumer.RegisteredAsync += (_, ea) =>
        {
            _logger.LogInformation("RabbitMQ consumer registered: {ConsumerTag}", ea.ConsumerTags);
            return Task.CompletedTask;
        };

        await _rmqChannel.BasicConsumeAsync(
            _queue ?? $"{_topic}:{_subscription}",
            autoAck: false,
            consumer: _rmqConsumer,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        _logger.LogInformation("RabbitMQListener started for {QueueOrTopic}.",
            _queue ?? $"{_topic}:{_subscription}");
    }

    private static string AsString(object? v) =>
        v switch
        {
            null => "",
            byte[] b => Encoding.UTF8.GetString(b),
            ReadOnlyMemory<byte> rom => Encoding.UTF8.GetString(rom.Span),
            string s => s,
            _ => v.ToString() ?? ""
        };

    private async Task RmqConsumer_ReceivedAsync(object sender, BasicDeliverEventArgs @event)
    {
        if (_stopping || @event.CancellationToken.IsCancellationRequested)
            return;

        // If the channel is gone because we’re stopping, bail early
        if (_rmqChannel is null)
            return;

        try
        {
            var body = Encoding.UTF8.GetString(@event.Body.Span);

            var headerDict = @event.BasicProperties.Headers ?? new Dictionary<string, object?>();
            var allHeaders = headerDict.ToDictionary(kvp => kvp.Key, kvp => AsString(kvp.Value));

            if (!allHeaders.TryGetValue("$type", out var typeHeader) || string.IsNullOrWhiteSpace(typeHeader))
            {
                _logger.LogWarning("Received message without $type header. DeliveryTag={DeliveryTag}", @event.DeliveryTag);
                typeHeader = "";
            }

            if (!allHeaders.TryGetValue("$logical", out var logicalHeader) || string.IsNullOrWhiteSpace(logicalHeader))
            {
                _logger.LogWarning("Received message without $logical header. DeliveryTag={DeliveryTag}", @event.DeliveryTag);
                logicalHeader = "";
            }

            var headers = allHeaders
                .Where(e => e.Key != "$type" && e.Key != "$logical")
                .ToImmutableDictionary();

            var actions = new RabbitMQMessageActions(_logger, _rmqChannel, @event.DeliveryTag, @event.Redelivered);

            var subscription = _subscription is not null
                ? $"{logicalHeader}/{_subscription}"
                : logicalHeader;

            var message = new ReceivedMessage()
            {
                MessageId = @event.BasicProperties.MessageId ?? @event.DeliveryTag.ToString(),
                Type = typeHeader,
                Body = body,
                Headers = headers,
                LogicalSourceName = logicalHeader,
                SubscriptionName = subscription,
                Actions = actions,
            };

            await _channel.Writer.WriteAsync(message, @event.CancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while receiving a RabbitMQ message (DeliveryTag={DeliveryTag})", @event.DeliveryTag);

            if (_rmqChannel is not null && !_stopping && !@event.CancellationToken.IsCancellationRequested)
            {
                try
                {
                    await _rmqChannel.BasicNackAsync(@event.DeliveryTag, multiple: false, requeue: !@event.Redelivered, @event.CancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex2)
                {
                    _logger.LogError(ex2, "Failed to nack message DeliveryTag={DeliveryTag}", @event.DeliveryTag);
                }
            }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _stopping = true;

        if (_rmqConsumer is not null)
        {
            _rmqConsumer.ReceivedAsync -= RmqConsumer_ReceivedAsync;
            _rmqConsumer = null;
        }

        // Complete our outgoing channel so downstream pumps exit
        _channel.Writer.TryComplete();

        if (_rmqChannel is not null)
        {
            try
            {
                await _rmqChannel.DisposeAsync()
                    .AsTask()
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ channel.");
            }

            _rmqChannel = null;
        }

        if (_rmqConnection is not null)
        {
            try
            {
                await _rmqConnection.DisposeAsync()
                    .AsTask()
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disposing RabbitMQ connection.");
            }

            _rmqConnection = null;
        }

        _logger.LogInformation("RabbitMQListener stopped for {QueueOrTopic}.", _queue ?? $"{_topic}:{_subscription}");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                try { StopAsync().GetAwaiter().GetResult(); } catch { /* ignore */ }
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
