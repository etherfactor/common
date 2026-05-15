using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class UnitOfWorkReference : IUnitOfWork
{
    private bool _disposed = false;

    public IUnitOfWork Inner { get; }

    public IServiceProvider Services => Inner.Services;

    public UnitOfWorkReference(
        IUnitOfWork inner)
    {
        Inner = inner;
    }

    /// <inheritdoc/>
    public int SaveChanges()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return 0; /* Intentional no-op, the original UoW is responsible */
    }

    /// <inheritdoc/>
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Task.FromResult(0); /* Intentional no-op, the original UoW is responsible */
    }

    /// <inheritdoc/>
    public IRepository<TEntity> Repository<TEntity>()
        where TEntity : class, IEntity
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Inner.Repository<TEntity>();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
