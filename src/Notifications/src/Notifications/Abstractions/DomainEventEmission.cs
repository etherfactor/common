namespace EtherGizmos.Common.Abstractions;

public record DomainEventEmission(IDomainEvent Event, IEnumerable<AudienceKey> Audience);
