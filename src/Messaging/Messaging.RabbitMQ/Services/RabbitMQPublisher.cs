using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using System.Text;
using System.Threading.Channels;

namespace EtherGizmos.Common.Services;

internal class RabbitMQPublisher : IMessagePublisher, IDisposable
{
    private readonly ILogger _logger;
    private readonly ConnectionFactory _rmqConnectionFactory;
    private readonly string? _queue;
    private readonly string? _topic;

    private readonly Channel<SentMessage> _channel = System.Threading.Channels.Channel.CreateUnbounded<SentMessage>();

    private IConnection? _rmqConnection;
    private IChannel? _rmqChannel;
    private CancellationTokenSource? _publishCts;
    private Task? _publishTask;

    private bool _disposed;

    public ChannelWriter<SentMessage> Channel => _channel;

    public RabbitMQPublisher(
        ILogger<RabbitMQPublisher> logger,
        ConnectionFactory connectionFactory,
        string queue)
    {
        _logger = logger;
        _rmqConnectionFactory = connectionFactory;
        _queue = queue;
    }

    public RabbitMQPublisher(
        ILogger<RabbitMQPublisher> logger,
        ConnectionFactory connectionFactory,
        string topic,
        string subscription)
    {
        _logger = logger;
        _rmqConnectionFactory = connectionFactory;
        _topic = topic;
    }

    public async Task StartAsync(
        CancellationToken cancellationToken = default)
    {
        _rmqConnection = await _rmqConnectionFactory.CreateConnectionAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        _rmqConnection.CallbackExceptionAsync += (_, e) =>
        {
            _logger.LogError(e.Exception, "Publisher connection callback exception.");
            return Task.CompletedTask;
        };
        _rmqConnection.ConnectionShutdownAsync += (_, e) =>
        {
            _logger.LogWarning("Publisher connection shutdown: {ReplyText} ({ReplyCode})", e.ReplyText, (int)e.ReplyCode);
            return Task.CompletedTask;
        };

        _rmqChannel = await _rmqConnection.CreateChannelAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
        _rmqChannel.CallbackExceptionAsync += (_, e) =>
        {
            _logger.LogError(e.Exception, "Publisher channel callback exception."); return
            Task.CompletedTask;
        };
        _rmqChannel.ChannelShutdownAsync += (_, e) =>
        {
            _logger.LogWarning("Publisher channel shutdown: {ReplyText} ({ReplyCode})", e.ReplyText, (int)e.ReplyCode);
            return Task.CompletedTask;
        };

        if (_queue is not null)
        {
            await _rmqChannel.QueueDeclareAsync(_queue, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await _rmqChannel.ExchangeDeclareAsync(_topic!, ExchangeType.Fanout, durable: true, autoDelete: false, arguments: null, cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        // Handle unroutable messages when 'mandatory: true'
        _rmqChannel.BasicReturnAsync += (_, ea) =>
        {
            try
            {
                var msg = Encoding.UTF8.GetString(ea.Body.Span);
                _logger.LogError("RabbitMQ BasicReturn: replyCode={ReplyCode}, replyText={ReplyText}, exchange={Exchange}, routingKey={RoutingKey}, bodyLength={Length}",
                    (int)ea.ReplyCode, ea.ReplyText, ea.Exchange, ea.RoutingKey, ea.Body.Length);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling BasicReturn.");
            }
            return Task.CompletedTask;
        };

        _publishCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _publishTask = ExecuteLoopAsync(_publishCts.Token);
        _logger.LogInformation("RabbitMQPublisher started for {QueueOrTopic}.", _queue ?? _topic!);
    }

    public async Task StopAsync(
        CancellationToken cancellationToken = default)
    {
        _publishCts?.Cancel();

        if (_publishTask is not null)
        {
            try { await _publishTask.ConfigureAwait(false); }
            catch (OperationCanceledException) when (_publishCts?.IsCancellationRequested == true) { }
            catch (Exception ex) { _logger.LogWarning(ex, "Publish loop ended with error."); }
        }

        _channel.Writer.TryComplete();
        try { await _channel.Reader.Completion.ConfigureAwait(false); } catch { /* ignore */ }

        if (_rmqChannel is not null)
        {
            try { await _rmqChannel.DisposeAsync().ConfigureAwait(false); } catch (Exception ex) { _logger.LogWarning(ex, "Error disposing publisher channel."); }
            _rmqChannel = null;
        }

        if (_rmqConnection is not null)
        {
            try { await _rmqConnection.DisposeAsync().ConfigureAwait(false); } catch (Exception ex) { _logger.LogWarning(ex, "Error disposing publisher connection."); }
            _rmqConnection = null;
        }

        _publishCts?.Dispose();
        _publishCts = null;

        _logger.LogInformation("RabbitMQPublisher stopped for {QueueOrTopic}.", _queue ?? _topic!);
    }

    private async Task ExecuteLoopAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("RabbitMQ publish loop starting for {QueueOrTopic}.", _queue ?? _topic!);

        while (!cancellationToken.IsCancellationRequested)
        {
            if (_rmqChannel is null)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                await foreach (var message in _channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                {
                    var properties = new BasicProperties()
                    {
                        Headers = message.AllHeaders.ToDictionary(kvp => kvp.Key, kvp => (object?)kvp.Value)
                    };

                    var bytes = Encoding.UTF8.GetBytes(message.Body);
                    if (_topic is not null)
                    {
                        await _rmqChannel.BasicPublishAsync(
                            exchange: _topic, routingKey: string.Empty, mandatory: true, body: bytes,
                            basicProperties: properties, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                    else
                    {
                        await _rmqChannel.BasicPublishAsync(
                            exchange: string.Empty, routingKey: _queue!, mandatory: true, body: bytes,
                            basicProperties: properties, cancellationToken: cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during RabbitMQ publish loop");
                await Task.Delay(2000, cancellationToken).ConfigureAwait(false);
            }
        }

        _logger.LogInformation("RabbitMQ publish loop exiting for {QueueOrTopic}.", _queue ?? _topic!);
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
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
