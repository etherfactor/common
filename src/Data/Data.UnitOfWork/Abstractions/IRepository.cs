namespace EtherGizmos.Common.Abstractions;

public interface IRepository { }

public interface IRepository<TEntity> : IRepository
    where TEntity : class, IEntity
{
    IQueryable<TEntity> Data { get; }

    void Add(TEntity entity);

    void Remove(TEntity entity);

    Task<TEntity> ReloadAsync(TEntity entity, CancellationToken cancellationToken = default);
}
