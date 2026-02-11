using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EtherGizmos.Common.Abstractions;

public interface IDomainEventExtractor
{
    bool CanHandle(
        EntityEntry entry);

    Task<IEnumerable<IDomainEvent>> ExtractAsync(
        EntityEntry entry,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);
}
