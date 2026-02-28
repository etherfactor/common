namespace EtherGizmos.Common.Abstractions;

public interface IDomainEventEmitter
{
    Task EmitAsync(
        IDomainEvent @event,
        IEnumerable<AudienceKey> audiences,
        CancellationToken cancellationToken = default);
}
