using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Abstractions;

public interface IDomainEventEmitter
{
    Task EmitAsync(
        IDomainEvent @event,
        IEnumerable<AudienceKey> audiences,
        DomainEventEmissionOptions? options = null,
        CancellationToken cancellationToken = default);
}
