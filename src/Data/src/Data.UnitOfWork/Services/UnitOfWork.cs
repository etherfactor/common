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

    public IDisposable? AmbientDisposable { get; set; }

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
        var scopes = new ConcurrentBag<TransactionScope>();
        var contexts = _contexts.Values.ToList();

        try
        {
            var total = 0;
            var exceptions = new ConcurrentBag<Exception>();

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 8,
            };

            foreach (var context in contexts)
            {
                context.Database.OpenConnection();
            }

            Parallel.ForEach(contexts, parallelOptions, (context) =>
            {
                try
                {
                    var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                    var count = context.SaveChanges();
                    Interlocked.Add(ref total, count);

                    scopes.Add(scope);
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

            Parallel.ForEach(scopes, parallelOptions, (scope) =>
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

            if (exceptions.Any())
            {
                throw new AggregateException(
                    "Encountered an exception while committing database changes. Data may be in an unexpected state.",
                    exceptions);
            }

            return total;
        }
        finally
        {
            foreach (var scope in scopes)
            {
                scope.Dispose();
            }

            foreach (var context in contexts)
            {
                try
                {
                    context.Database.CloseConnection();
                }
                catch { }
            }
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var scopes = new ConcurrentBag<TransactionScope>();
        var contexts = _contexts.Values.ToList();

        try
        {
            var total = 0;
            var exceptions = new ConcurrentBag<Exception>();

            var parallelOptions = new ParallelOptions()
            {
                MaxDegreeOfParallelism = 8,
                CancellationToken = cancellationToken,
            };

            foreach (var context in contexts)
            {
                await context.Database.OpenConnectionAsync(cancellationToken: cancellationToken);
            }

            await Parallel.ForEachAsync(contexts, parallelOptions, async (context, cancellationToken) =>
            {
                try
                {
                    var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

                    var count = await context.SaveChangesAsync(cancellationToken: cancellationToken);
                    Interlocked.Add(ref total, count);

                    scopes.Add(scope);
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

            await Parallel.ForEachAsync(scopes, parallelOptions, (scope, cancellationToken) =>
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

            if (exceptions.Any())
            {
                throw new AggregateException(
                    "Encountered an exception while committing database changes. Data may be in an unexpected state.",
                    exceptions);
            }

            return total;
        }
        finally
        {
            foreach (var scope in scopes)
            {
                scope.Dispose();
            }

            foreach (var context in contexts)
            {
                try
                {
                    await context.Database.CloseConnectionAsync();
                }
                catch { }
            }
        }
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
