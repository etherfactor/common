using EtherGizmos.Common.Abstractions;

namespace Notifications.Test;

internal class TestDomainEvent : IDomainEvent
{
    public int Value { get; set; }
}
