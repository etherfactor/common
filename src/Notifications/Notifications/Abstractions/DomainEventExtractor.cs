using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EtherGizmos.Common.Abstractions;

public abstract class DomainEventExtractor<TEntity> : IDomainEventExtractor
    where TEntity : class, IDomainEvent
{
    public bool CanHandle(
        EntityEntry entry)
        => entry.Entity is TEntity;

    public IAsyncEnumerable<DomainEventEmission> ExtractAsync(
        EntityEntry entry,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default)
        => ExtractAsync((EntityEntry<TEntity>)entry, unitOfWork, cancellationToken);

    protected abstract IAsyncEnumerable<DomainEventEmission> ExtractAsync(
        EntityEntry<TEntity> entry,
        IUnitOfWork unitOfWork,
        CancellationToken cancellationToken = default);
}
