using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace EtherGizmos.Common.Configuration;

public class UnitOfWorkOptions
{
    internal HashSet<Type> ContextTypes { get; } = [];
    internal ConcurrentDictionary<Type, Type> EntityContexts { get; } = [];

    public UnitOfWorkOptions BindDbContext<TContext>()
        where TContext : DbContext
    {
        if (!ContextTypes.Contains(typeof(TContext)))
        {
            lock (ContextTypes)
            {
                if (ContextTypes.Contains(typeof(TContext)))
                    return this;

                ContextTypes.Add(typeof(TContext));

                var entityTypes = typeof(TContext)
                    .GetProperties()
                    .Where(e => e.PropertyType.IsGenericType && e.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
                    .Select(e => e.PropertyType.GetGenericArguments()[0])
                    .Distinct()
                    .ToList();

                var existing = entityTypes.Where(EntityContexts.ContainsKey);
                if (existing.Any())
                {
                    throw new InvalidOperationException($"The following types are already mapped in another context and " +
                        $"cannot be mapped to {typeof(TContext)}:\r\n{string.Join(", ", existing)}");
                }

                foreach (var type in entityTypes)
                {
                    EntityContexts.GetOrAdd(type, typeof(TContext));
                }
            }
        }

        return this;
    }
}
