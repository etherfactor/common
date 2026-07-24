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
    private const int MAX_ROUNDS = 16;

    private readonly IOptions<UnitOfWorkOptions> _options;
    private readonly IServiceScope? _serviceScope;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkAccessor _uowAccessor;

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
        _uowAccessor = _serviceProvider.GetRequiredService<IUnitOfWorkAccessor>();
    }

    public UnitOfWork(
        IOptions<UnitOfWorkOptions> options,
        IServiceProvider serviceProvider)
    {
        _options = options;
        _serviceProvider = serviceProvider;
        _uowAccessor = _serviceProvider.GetRequiredService<IUnitOfWorkAccessor>();
    }

    public IRepository<TEntity> Repository<TEntity>()
        where TEntity : class, IEntity
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

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
        ObjectDisposedException.ThrowIf(_disposed, this);

        Task<int> task;
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(() =>
            {
                using var d = _uowAccessor.Enter(this);

                var scopes = new ConcurrentBag<TransactionScope>();
                var opened = new HashSet<DbContext>();

                try
                {
                    var total = 0;
                    var exceptions = new ConcurrentBag<Exception>();

                    var parallelOptions = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 8,
                    };

                    for (var round = 0; round < MAX_ROUNDS; round++)
                    {
                        var contexts = _contexts.Values
                            .Where(context => context.ChangeTracker.HasChanges())
                            .ToList();

                        if (contexts.Count == 0)
                            break;

                        foreach (var context in contexts)
                        {
                            if (opened.Add(context))
                                context.Database.OpenConnection();
                        }

                        Parallel.ForEach(contexts, parallelOptions, (context) =>
                        {
                            try
                            {
                                var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                                scopes.Add(scope);

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
                    }

                    if (_contexts.Values.Any(context => context.ChangeTracker.HasChanges()))
                    {
                        throw new InvalidOperationException(
                            "Unit of work save did not converge. Saving changes kept producing additional changes.");
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

                    foreach (var context in opened)
                    {
                        try
                        {
                            context.Database.CloseConnection();
                        }
                        catch { }
                    }
                }
            });
        }

        return task.Result;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        Task<int> task;
        using (ExecutionContext.SuppressFlow())
        {
            task = Task.Run(async () =>
            {
                using var d = _uowAccessor.Enter(this);

                var scopes = new ConcurrentBag<TransactionScope>();
                var opened = new HashSet<DbContext>();

                try
                {
                    var total = 0;
                    var exceptions = new ConcurrentBag<Exception>();

                    var saveOptions = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 8,
                        CancellationToken = cancellationToken,
                    };

                    var commitOptions = new ParallelOptions()
                    {
                        MaxDegreeOfParallelism = 8,
                    };

                    for (var round = 0; round < MAX_ROUNDS; round++)
                    {
                        var contexts = _contexts.Values
                            .Where(e => e.ChangeTracker.HasChanges())
                            .ToList();

                        if (contexts.Count == 0)
                            break;

                        foreach (var context in contexts)
                        {
                            if (opened.Add(context))
                                await context.Database.OpenConnectionAsync(cancellationToken: cancellationToken);
                        }

                        await Parallel.ForEachAsync(contexts, saveOptions, async (context, cancellationToken) =>
                        {
                            try
                            {
                                var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
                                scopes.Add(scope);

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
                    }

                    if (_contexts.Values.Any(context => context.ChangeTracker.HasChanges()))
                    {
                        throw new InvalidOperationException(
                            "Unit of work save did not converge. Saving changes kept producing additional changes.");
                    }

                    await Parallel.ForEachAsync(scopes, commitOptions, (scope, cancellationToken) =>
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

                    foreach (var context in opened)
                    {
                        try
                        {
                            await context.Database.CloseConnectionAsync();
                        }
                        catch { }
                    }
                }
            });
        }

        return await task;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                AmbientDisposable?.Dispose();
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
