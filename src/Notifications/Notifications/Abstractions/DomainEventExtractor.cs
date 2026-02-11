using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EtherGizmos.Common.Abstractions;

public abstract class DomainEventExtractor<TEntity> : IDomainEventExtractor
    where TEntity : class, IDomainEvent
{
    public bool CanHandle(
        EntityEntry entry)
        => entry.Entity is TEntity;

    public Task<IEnumerable<IDomainEvent>> ExtractAsync(
        EntityEntry entry,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
        => ExtractAsync((EntityEntry<TEntity>)entry, unitOfWork, cancellationToken);

    protected abstract Task<IEnumerable<IDomainEvent>> ExtractAsync(
        EntityEntry<TEntity> entry,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);
}
