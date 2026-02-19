using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface IDomainEventRouter<TEvent>
    where TEvent : IDomainEvent
{
    IQueryable<NotificationSubscription> FilterScope(
        IUnitOfWork uow,
        IQueryable<NotificationSubscription> queryable,
        TEvent @event);
}
