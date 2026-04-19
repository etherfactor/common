using EtherGizmos.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class EfCoreRepository<TEntity> : IRepository<TEntity>
    where TEntity : class, IEntity
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DbSet<TEntity> _entities;

    public EfCoreRepository(
        IServiceProvider serviceProvider,
        DbSet<TEntity> entities)
    {
        _serviceProvider = serviceProvider;
        _entities = entities;
    }

    public IQueryable<TEntity> Data => _entities;

    public void Add(
        TEntity entity)
    {
        _entities.Add(entity);
    }

    public void Remove(
        TEntity entity)
    {
        var entry = _entities.Entry(entity);
        if (entry.State == EntityState.Detached)
            _entities.Attach(entity);

        _entities.Remove(entity);
    }

    public async Task<TEntity> ReloadAsync(
        TEntity entity,
        CancellationToken cancellationToken = default)
    {
        var context = _serviceProvider.GetRequiredKeyedService<DbContext>(typeof(TEntity));
        var entry = context.Entry(entity);

        var key = context.Entry(entity).Metadata.FindPrimaryKey()!;

        var keyValues = key.Properties
            .Select(prop => entry.Property(prop).CurrentValue)
            .ToArray();

        entry.State = EntityState.Detached;

        var newEntity = await _entities.FindAsync(keyValues, cancellationToken);
        return newEntity
            ?? throw new InvalidOperationException("Failed to find entity");
    }
}
