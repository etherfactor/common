using EtherGizmos.Common.Abstractions;
using System.Runtime.CompilerServices;

namespace Notifications.Test;

internal class TestDomainEvent : IDomainEvent
{
    public int Value { get; set; }
}

internal class TestDomainEventRouter : IDomainEventRouter<TestDomainEvent>
{
    public async IAsyncEnumerable<string> FilterScopeAsync(
        TestDomainEvent @event,
        IEnumerable<AudienceKey> audiences,
        IEnumerable<string> userIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var userId in userIds)
        {
            if (userId.Equals("ABC", StringComparison.OrdinalIgnoreCase))
            {
                yield return userId;
            }
        }
    }
}
