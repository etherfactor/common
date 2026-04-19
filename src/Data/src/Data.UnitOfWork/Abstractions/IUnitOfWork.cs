namespace EtherGizmos.Common.Abstractions;

public interface IUnitOfWork : IDisposable
{
    IServiceProvider Services { get; }

    int SaveChanges();

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    IRepository<TEntity> Repository<TEntity>()
        where TEntity : class, IEntity;
}
