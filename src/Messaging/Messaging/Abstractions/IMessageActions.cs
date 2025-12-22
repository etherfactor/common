namespace EtherGizmos.Common.Abstractions;

public interface IMessageActions
{
    bool Invoked { get; }

    MessageDecision Decision { get; }

    Task AbandonAsync(CancellationToken cancellationToken = default);

    Task CompleteAsync(CancellationToken cancellationToken = default);

    Task DeadLetterAsync(CancellationToken cancellationToken = default);
}
