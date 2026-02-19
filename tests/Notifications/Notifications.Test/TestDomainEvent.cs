using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;

namespace Notifications.Test;

internal class TestDomainEvent : IDomainEvent
{
    public int Value { get; set; }
}

internal class TestDomainEventRouter : IDomainEventRouter<TestDomainEvent>
{
    public IQueryable<NotificationSubscription> FilterScope(
        IUnitOfWork uow,
        IQueryable<NotificationSubscription> queryable,
        TestDomainEvent @event)
    {
        //Fire all events
        return queryable;
    }
}
