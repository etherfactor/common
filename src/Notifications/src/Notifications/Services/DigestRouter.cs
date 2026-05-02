using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using System.Runtime.CompilerServices;

namespace EtherGizmos.Common.Services;

internal class DigestRouter<TEvent>
    : IDomainEventRouter<Digest<TEvent>>
    where TEvent : class, IDomainEvent
{
    public async IAsyncEnumerable<string> FilterScopeAsync(
        Digest<TEvent> @event,
        IEnumerable<AudienceKey> audiences,
        IEnumerable<string> userIds,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var userSet = userIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var self = audiences
            .Where(e => e.Kind == "$self")
            .Select(e => e.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase);

        foreach (var userId in self)
        {
            if (userSet.Contains(userId))
                yield return userId;
        }
    }
}
