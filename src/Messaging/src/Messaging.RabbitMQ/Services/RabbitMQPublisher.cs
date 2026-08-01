using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Diagnostics;
using System.Text;

namespace EtherGizmos.Common.Services;

internal sealed class RabbitMQPublisher : IMessagePublisherTransport, IAsyncDisposable
{
    private readonly ILogger _logger;
    private readonly ConnectionFactory _connectionFactory;
    private readonly string? _queue;
    private readonly string? _topic;

    private readonly SemaphoreSlim _stopGate = new(1, 1);

    private IConnection? _connection;
    private IChannel? _channel;
    private bool _stopped;

    public RabbitMQPublisher(
        ILogger<RabbitMQPublisher> logger,
        ConnectionFactory connectionFactory,
        string queue)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _queue = queue;
    }

    public RabbitMQPublisher(
        ILogger<RabbitMQPublisher> logger,
        ConnectionFactory connectionFactory,
        string topic,
        string subscription)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _topic = topic;
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
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

            _channel.CallbackExceptionAsync += OnChannelCallbackExceptionAsync;
            _channel.ChannelShutdownAsync += OnChannelShutdownAsync;
            _channel.BasicReturnAsync += OnBasicReturnAsync;

            if (_queue is not null)
            {
                await _channel.QueueDeclareAsync(
                    queue: _queue,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await _channel.ExchangeDeclareAsync(
                    exchange: _topic!,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false,
                    arguments: null,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation(
                "RabbitMQ publisher started for {QueueOrTopic}.",
                DisplayName);
        }
        catch
        {
            await StopAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
    }

    public async Task PublishAsync(
        SentMessage message,
        CancellationToken cancellationToken = default)
    {
        var channel = _channel
            ?? throw new InvalidOperationException(
                $"RabbitMQ publisher '{DisplayName}' is not started.");

        using var activity = ActivitySources.Messaging.StartActivityFromCarrier(
            $"Publish {message.Type} to {message.LogicalDestinationName}",
            ActivityKind.Producer,
            message.Headers);

        activity?.SetTag("messaging.operation.name", "publish");
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination.name", message.LogicalDestinationName);
        activity?.SetTag("messaging.message.type", message.Type);

        message = message.AddActivityHeaders(activity);

        var properties = new BasicProperties
        {
            MessageId = message.MessageId,
            Headers = message.AllHeaders.ToDictionary(
                pair => pair.Key,
                pair => (object?)pair.Value),
        };

        var body = Encoding.UTF8.GetBytes(message.Body);

        if (_topic is not null)
        {
            await channel.BasicPublishAsync(
                exchange: _topic,
                routingKey: string.Empty,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await channel.BasicPublishAsync(
                exchange: string.Empty,
                routingKey: _queue!,
                mandatory: true,
                basicProperties: properties,
                body: body,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _stopGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_stopped)
                return;

            if (_channel is not null)
            {
                _channel.CallbackExceptionAsync -= OnChannelCallbackExceptionAsync;
                _channel.ChannelShutdownAsync -= OnChannelShutdownAsync;
                _channel.BasicReturnAsync -= OnBasicReturnAsync;

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
                    _logger.LogWarning(ex, "Error disposing RabbitMQ publisher channel.");
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
                    _logger.LogWarning(ex, "Error disposing RabbitMQ publisher connection.");
                }

                _connection = null;
            }

            _stopped = true;

            _logger.LogInformation(
                "RabbitMQ publisher stopped for {QueueOrTopic}.",
                DisplayName);
        }
        finally
        {
            _stopGate.Release();
        }
    }

    private Task OnConnectionCallbackExceptionAsync(
        object sender,
        CallbackExceptionEventArgs args)
    {
        _logger.LogError(args.Exception, "RabbitMQ publisher connection callback exception.");
        return Task.CompletedTask;
    }

    private Task OnConnectionShutdownAsync(
        object sender,
        ShutdownEventArgs args)
    {
        _logger.LogWarning(
            "RabbitMQ publisher connection shutdown: {ReplyText} ({ReplyCode}).",
            args.ReplyText,
            (int)args.ReplyCode);
        return Task.CompletedTask;
    }

    private Task OnChannelCallbackExceptionAsync(
        object sender,
        CallbackExceptionEventArgs args)
    {
        _logger.LogError(args.Exception, "RabbitMQ publisher channel callback exception.");
        return Task.CompletedTask;
    }

    private Task OnChannelShutdownAsync(
        object sender,
        ShutdownEventArgs args)
    {
        _logger.LogWarning(
            "RabbitMQ publisher channel shutdown: {ReplyText} ({ReplyCode}).",
            args.ReplyText,
            (int)args.ReplyCode);
        return Task.CompletedTask;
    }

    private Task OnBasicReturnAsync(
        object sender,
        BasicReturnEventArgs args)
    {
        _logger.LogError(
            "RabbitMQ returned an unroutable message: replyCode={ReplyCode}, replyText={ReplyText}, exchange={Exchange}, routingKey={RoutingKey}, bodyLength={Length}.",
            (int)args.ReplyCode,
            args.ReplyText,
            args.Exchange,
            args.RoutingKey,
            args.Body.Length);
        return Task.CompletedTask;
    }

    private string DisplayName => _queue ?? _topic!;

    public async ValueTask DisposeAsync()
    {
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        _stopGate.Dispose();
    }
}
