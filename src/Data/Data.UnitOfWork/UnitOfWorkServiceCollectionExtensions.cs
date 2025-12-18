using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.EntityFrameworkCore;
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
}
