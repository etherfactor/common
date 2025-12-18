namespace EtherGizmos.Common.Abstractions;

public interface IRepository { }

public interface IRepository<TEntity> : IRepository
    where TEntity : class, IEntity
{
    IQueryable<TEntity> Data { get; }

    void Attach(TEntity entity);

    void Create(TEntity entity);

    void Delete(TEntity entity);

    void Detach(TEntity entity);

    Task<TEntity> ReloadAsync(TEntity entity, CancellationToken cancellationToken = default);
}
