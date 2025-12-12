using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class MessageContext<TMessage> : IMessageContext<TMessage>
    where TMessage : class, new()
{
    public TMessage Message { get; }

    public IMessageActions Actions { get; }

    public CancellationToken CancellationToken { get; }

    public MessageContext(
        TMessage message,
        IMessageActions actions,
        CancellationToken cancellationToken)
    {
        Message = message;
        Actions = actions;
        CancellationToken = cancellationToken;
    }
}
