using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common.Services;

internal class SettlementMessageActions : IMessageActions
{
    private readonly ILogger _logger;
    private readonly IMessageSettlement _settlement;
    private readonly ReceivedMessage _message;

    public bool Invoked { get; private set; }

    public MessageDecision Decision { get; private set; }

    public SettlementMessageActions(
        ILogger logger,
        IMessageSettlement settlement,
        ReceivedMessage message)
    {
        _logger = logger;
        _settlement = settlement;
        _message = message;
    }

    public async Task AbandonAsync(CancellationToken cancellationToken = default)
    {
        if (Invoked)
            throw new InvalidOperationException("Already performed an action on this message.");

        Invoked = true;
        Decision = MessageDecision.Abandon;

        _logger.LogInformation("Abandoning message {MessageId}", _message.MessageId);

        _settlement.Abandon(_message.ConsumerName!);
    }

    public async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (Invoked)
            throw new InvalidOperationException("Already performed an action on this message.");

        Invoked = true;
        Decision = MessageDecision.Complete;

        _logger.LogInformation("Completing message {MessageId}", _message.MessageId);

        _settlement.Complete(_message.ConsumerName!);
    }

    public async Task DeadLetterAsync(CancellationToken cancellationToken = default)
    {
        if (Invoked)
            throw new InvalidOperationException("Already performed an action on this message.");

        Invoked = true;
        Decision = MessageDecision.DeadLetter;

        _logger.LogInformation("Dead lettering message {MessageId}", _message.MessageId);

        _settlement.DeadLetter(_message.ConsumerName!);
    }
}
