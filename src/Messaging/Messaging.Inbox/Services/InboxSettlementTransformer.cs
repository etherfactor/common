using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Logging;

namespace EtherGizmos.Common.Services;

internal class InboxSettlementTransformer : IMessageTransformer
{
    public ILogger _logger;
    private readonly IMessageSettlement _settlement;

    public InboxSettlementTransformer(
        ILogger<InboxSettlementMiddleware> logger,
        IMessageSettlement settlement)
    {
        _logger = logger;
        _settlement = settlement;
    }

    public Task<ReceivedMessage> UnwrapAsync(ReceivedMessage envelope, CancellationToken cancellationToken = default)
    {
        _settlement.Register(envelope.ConsumerName ?? throw new InvalidOperationException("Expected a consumer name"), envelope.Actions);

        envelope = envelope with
        {
            Actions = new SettlementMessageActions(_logger, _settlement, envelope),
        };

        return Task.FromResult(envelope);
    }

    public Task<SentMessage> WrapAsync(SentMessage envelope, CancellationToken cancellationToken = default)
        => Task.FromResult(envelope);
}
