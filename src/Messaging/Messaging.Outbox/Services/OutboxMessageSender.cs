using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class OutboxMessageSender : IMessageSender
{
    private readonly IMessageSender _inner;

    public IServiceProvider Services => _inner.Services;

    public OutboxMessageSender(
        IMessageSender inner)
    {
        _inner = inner;
    }

    public Task SendAsync(
        SentMessage message,
        CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
