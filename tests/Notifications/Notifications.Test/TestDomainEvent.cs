using EtherGizmos.Common.Abstractions;

namespace Notifications.Test;

internal class TestDomainEvent : IDomainEvent
{
    public string EventTypeId => "test.domain.event";
}
