using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common;

public static class DomainEventEmitterExtensions
{
    extension(IDomainEventEmitter @this)
    {
        public Task EmitAsync(
            IDomainEvent @event,
            AudienceKey audience,
            DomainEventEmissionOptions? options = null,
            CancellationToken cancellationToken = default)
            => @this.EmitAsync(
                @event,
                [audience],
                options,
                cancellationToken);
    }
}
