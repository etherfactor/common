using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using System.Diagnostics;

namespace EtherGizmos.Common.Services;

internal class OutboxMessageSender : IMessageSender
{
    private readonly IMessageSender _inner;
    private readonly IOutboxSignal _signal;
    private readonly IUnitOfWorkFactory _uowFactory;

    public IServiceProvider Services => _inner.Services;

    public OutboxMessageSender(
        IMessageSender inner,
        IOutboxSignal signal,
        IUnitOfWorkFactory uowFactory)
    {
        _inner = inner;
        _signal = signal;
        _uowFactory = uowFactory;
    }

    public async Task SendAsync(
        SentMessage message,
        CancellationToken cancellationToken = default)
    {
        //Extract the current activity context
        var activity = Activity.Current;
        var headers = new Dictionary<string, string>();
        if (activity is not null)
        {
            DistributedContextPropagator.Current.Inject(activity, headers, (c, key, value) =>
            {
                var headers = (Dictionary<string, string>)c!;
                headers[key] = value;
            });
        }

        //Add that context to the message
        message = message with
        {
            Headers = message.Headers.AddRange(headers),
        };

        using var uow = _uowFactory.Create();
        var messageRepo = uow.Repository<OutboxMessage>();

        var messageRecord = new OutboxMessage()
        {
            MessageId = Guid.NewGuid().ToString("N"),
            QueuedAt = DateTimeOffset.UtcNow,
            AvailableAt = DateTimeOffset.UtcNow,
            Status = OutboxStatusType.Pending,
            LogicalDestinationName = message.LogicalDestinationName,
            Type = message.Type,
            Payload = message.Body,
            Headers = message.Headers.ToDictionary(),
        };

        messageRepo.Add(messageRecord);

        await uow.SaveChangesAsync(cancellationToken);

        _signal.Pulse();
    }
}
