using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common.Services;

internal class InboxSettlementTransformer : IMessageTransformer
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IOptionsMonitor<MessagingOptions> _options;
    private readonly IMessageSettlement _settlement;

    public InboxSettlementTransformer(
        ILogger<InboxSettlementMiddleware> logger,
        IServiceProvider serviceProvider,
        IOptionsMonitor<MessagingOptions> options,
        IMessageSettlement settlement)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _options = options;
        _settlement = settlement;
    }

    public Task<ReceivedMessage> UnwrapAsync(ReceivedMessage envelope, CancellationToken cancellationToken = default)
    {
        _settlement.SetExpected(() =>
        {
            var type = _options.CurrentValue.ConvertType(envelope.Type);
            var consumerType = typeof(IMessageConsumer<>).MakeGenericType(type);
            var enumerableType = typeof(IEnumerable<>).MakeGenericType(consumerType);

            var consumers = (IEnumerable<object>)_serviceProvider.GetRequiredService(enumerableType);
            var count = consumers.Count();

            return count;
        });

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
