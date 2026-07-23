using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EtherGizmos.Common.Services;

internal class RabbitMQMessageActions : IMessageActions
{
    private readonly ILogger _logger;
    private readonly IChannel _channel;
    private readonly ulong _messageId;
    private readonly bool _redelivered;

    public bool Invoked { get; private set; }

    public MessageDecision Decision { get; private set; }

    public RabbitMQMessageActions(
        ILogger logger,
        IChannel channel,
        ulong messageId,
        bool redelivered)
    {
        _logger = logger;
        _channel = channel;
        _messageId = messageId;
        _redelivered = redelivered;
    }

    public async Task AbandonAsync(CancellationToken cancellationToken = default)
    {
        if (Invoked)
            throw new InvalidOperationException("Already performed an action on this message.");

        Invoked = true;
        Decision = MessageDecision.Abandon;

        _logger.LogInformation("Abandoning message {MessageId}", _messageId);

        await _channel.BasicNackAsync(_messageId, false, !_redelivered, cancellationToken);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (Invoked)
            throw new InvalidOperationException("Already performed an action on this message.");

        Invoked = true;
        Decision = MessageDecision.Complete;

        _logger.LogInformation("Completing message {MessageId}", _messageId);

        await _channel.BasicAckAsync(_messageId, false, cancellationToken);
    }

    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        if (Invoked)
            throw new InvalidOperationException("Already performed an action on this message.");

        Invoked = true;
        Decision = MessageDecision.DeadLetter;

        _logger.LogInformation("Dead lettering message {MessageId}", _messageId);

        await _channel.BasicNackAsync(_messageId, false, false, cancellationToken);
    }
}
