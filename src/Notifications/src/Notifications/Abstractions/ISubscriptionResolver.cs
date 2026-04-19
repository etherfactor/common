using EtherGizmos.Common.Models;

namespace EtherGizmos.Common.Abstractions;

public interface ISubscriptionResolver
{
    Task<IReadOnlyList<NotificationSubscription>> ResolveAsync(
        IDomainEvent @event,
        CancellationToken cancellationToken = default);
}
