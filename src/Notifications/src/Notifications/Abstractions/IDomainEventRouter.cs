namespace EtherGizmos.Common.Abstractions;

public interface IDomainEventRouter<TEvent>
    where TEvent : IDomainEvent
{
    IAsyncEnumerable<string> FilterScopeAsync(
        TEvent @event,
        IEnumerable<AudienceKey> audiences,
        IEnumerable<string> userIds,
        CancellationToken cancellationToken = default);
}
