namespace EtherGizmos.Common.Abstractions;

public interface IMessageContext<TMessage>
    where TMessage : class, new()
{
    TMessage Message { get; }

    IMessageActions Actions { get; }

    CancellationToken CancellationToken { get; }
}
