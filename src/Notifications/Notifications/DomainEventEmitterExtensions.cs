using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class DomainEventEmitterExtensions
{
    extension(IDomainEventEmitter @this)
    {
        public Task EmitAsync(
            IDomainEvent @event,
            AudienceKey audience,
            CancellationToken cancellationToken = default)
            => @this.EmitAsync(@event, [audience], cancellationToken);
    }
}
