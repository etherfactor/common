namespace EtherGizmos.Common.Abstractions;

public interface IDomainEventEmitter
{
    Task EmitAsync(
        IDomainEvent @event,
        CancellationToken cancellationToken = default);
}
