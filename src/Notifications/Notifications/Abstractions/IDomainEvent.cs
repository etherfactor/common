namespace EtherGizmos.Common.Abstractions;

public interface IDomainEvent
{
    public string EventTypeId { get; }
}
