namespace EtherGizmos.Common.Abstractions;

public interface IDomainEvent
{
    public Guid EventInstanceId { get; }

    public string EventTypeId { get; }
}
