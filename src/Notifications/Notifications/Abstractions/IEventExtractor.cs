using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EtherGizmos.Common.Abstractions;

public interface IEventExtractor
{
    bool CanHandle(
        EntityEntry entry);

    Task<IEnumerable<IDomainEvent>> ExtractAsync(
        EntityEntry entry,
        CancellationToken cancellationToken = default);
}
