using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Transactions;

namespace EtherGizmos.Common.Services;

internal class UnitOfWork : IUnitOfWork
{
    private readonly IOptions<UnitOfWorkOptions> _options;
    private readonly IServiceScope? _serviceScope;
    private readonly IServiceProvider _serviceProvider;

    private readonly ConcurrentDictionary<Type, DbContext> _contexts = [];

    private bool _disposed;

    public IServiceProvider Services => _serviceProvider;

    public UnitOfWork(
        IOptions<UnitOfWorkOptions> options,
        IServiceScope serviceScope)
        : this(options, serviceScope.ServiceProvider)
    {
        _serviceScope = serviceScope;
    }

    public UnitOfWork(
        IOptions<UnitOfWorkOptions> options,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
    }

    public IRepository<TEntity> Repository<TEntity>()
        where TEntity : class, IEntity
    {
        LoadContext<TEntity>();

        var repository = _serviceProvider.GetRequiredService<IRepository<TEntity>>();
        return repository;
    }

    private DbContext LoadContext<TEntity>()
        where TEntity : class
    {
        var contextType = _options.Value.EntityContexts[typeof(TEntity)];
        var context = _contexts.GetOrAdd(contextType, type =>
        {
            var context = (DbContext)_serviceProvider.GetRequiredService(type);

            return context;
        });

        return context;
    }

    public int SaveChanges()
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var total = 0;
        var exceptions = new ConcurrentBag<Exception>();

        var parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 8,
        };

        var contexts = _contexts.Values.ToList();
        foreach (var context in contexts)
        {
            context.Database.OpenConnection();
        }

        Parallel.ForEach(_contexts.Values, parallelOptions, (context) =>
        {
            try
            {
                var count = context.SaveChanges();
                Interlocked.Add(ref total, count);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        if (exceptions.Any())
        {
            throw new AggregateException(
                "Encountered an exception while saving database changes.",
                exceptions);
        }

        Parallel.ForEach(_contexts.Values, parallelOptions, (context) =>
        {
            try
            {
                scope.Complete();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        foreach (var context in contexts)
        {
            context.Database.CloseConnection();
        }

        if (exceptions.Any())
        {
            throw new AggregateException(
                "Encountered an exception while committing database changes. Data may be in an unexpected state.",
                exceptions);
        }

        return total;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var total = 0;
        var exceptions = new ConcurrentBag<Exception>();

        var parallelOptions = new ParallelOptions()
        {
            MaxDegreeOfParallelism = 8,
            CancellationToken = cancellationToken,
        };

        var contexts = _contexts.Values.ToList();
        foreach (var context in contexts)
        {
            await context.Database.OpenConnectionAsync(cancellationToken: cancellationToken);
        }

        await Parallel.ForEachAsync(_contexts.Values, parallelOptions, async (context, cancellationToken) =>
        {
            try
            {
                var count = await context.SaveChangesAsync(cancellationToken: cancellationToken);
                Interlocked.Add(ref total, count);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        if (exceptions.Any())
        {
            throw new AggregateException(
                "Encountered an exception while saving database changes.",
                exceptions);
        }

        await Parallel.ForEachAsync(_contexts.Values, parallelOptions, (context, cancellationToken) =>
        {
            try
            {
                scope.Complete();
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }

            return ValueTask.CompletedTask;
        });

        foreach (var context in contexts)
        {
            await context.Database.CloseConnectionAsync();
        }

        if (exceptions.Any())
        {
            throw new AggregateException(
                "Encountered an exception while committing database changes. Data may be in an unexpected state.",
                exceptions);
        }

        return total;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _serviceScope?.Dispose();
            }

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
