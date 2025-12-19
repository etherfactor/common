using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EtherGizmos.Common;

public static class UnitOfWorkServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        public IServiceCollection AddUnitOfWork(
            Action<UnitOfWorkOptions> configureOptions)
        {
            @this.AddSingleton<IUnitOfWorkFactory, UnitOfWorkFactory>()
                .AddScoped(typeof(IRepository<>), typeof(EfCoreRepository<>));

            @this.AddOptions<UnitOfWorkOptions>()
                .Configure(configureOptions);

            var options = new UnitOfWorkOptions();
            configureOptions(options);

            foreach (var pair in options.EntityContexts)
            {
                var entityType = pair.Key;
                var contextType = pair.Value;
                var serviceType = typeof(DbSet<>).MakeGenericType(entityType);

                @this.AddKeyedScoped(typeof(DbContext), entityType, contextType)
                    .AddScoped(serviceType, provider =>
                    {
                        var context = (DbContext)provider.GetRequiredService(contextType);
                        return context
                            .GetType()
                            .GetMethod(nameof(DbContext.Set), BindingFlags.Instance | BindingFlags.Public, [])!
                            .MakeGenericMethod(entityType)
                            .Invoke(context, [])!;
                    });
            }

            return @this;
        }
    }

    extension(DbContextOptionsBuilder @this)
    {
        public DbContextOptionsBuilder UseConnection(
            IServiceProvider provider,
            string connectionId,
            Action<IRelationalEfOptions>? optionsBuilder = null)
        {
            var resolver = provider.GetRequiredService<IConnectionResolver>();
            var connection = resolver.GetDatabaseConnection(connectionId);
            var type = connection.GetType();

            typeof(UnitOfWorkServiceCollectionExtensions)
                .GetMethod(nameof(InnerUseConnection), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod([type])
                .Invoke(null, [@this, provider, @this, connection, optionsBuilder]);

            return @this;
        }

        internal void InnerUseConnection<TOptions>(
            IServiceProvider provider,
            DbContextOptionsBuilder builder,
            TOptions options,
            Action<IRelationalEfOptions>? optionsBuilder)
            where TOptions : DatabaseConnectionOptions, new()
        {
            var factory = provider.GetService<IDbContextBuilder<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for configuring a migration runner for type {typeof(TOptions)}");

            factory.ConfigureContext(builder, options, optionsBuilder);
        }
    }
}
