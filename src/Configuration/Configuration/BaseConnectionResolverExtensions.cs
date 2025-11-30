using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common;

public static class BaseConnectionResolverExtensions
{
    extension(IConnectionResolver @this)
    {
        public TOptions GetConnection<TOptions>(
            string connectionId,
            string expectedType)
            where TOptions : class, new()
        {
            var options = @this.Options;
            var logger = @this.ServiceProvider.GetService<ILogger<ConnectionResolver>>()
                ?? NullLogger<ConnectionResolver>.Instance;

            if (options.TryGetValue(connectionId, out var connection))
            {
                if (connection.Type == expectedType)
                {
                    var configuration = @this.ServiceProvider
                        .GetRequiredService<IConfiguration>();

                    var section = configuration.GetSection($"Connections:{connectionId}");

                    var configuredTypes = @this.ServiceProvider
                        .GetRequiredService<IOptionsMonitor<ConnectionTypeOptions>>()
                        .CurrentValue;

                    var found = new List<TOptions>();
                    foreach (var type in configuredTypes.RootConnectionTypes)
                    {
                        var sectionAsType = section.Get(type)!;

                        var properties = sectionAsType.GetType().GetProperties()
                            .Where(e => e.PropertyType.IsAssignableTo(typeof(TOptions)))
                            .Select(e => (TOptions)e.GetValue(sectionAsType)!)
                            .Where(e => e is not null)
                            .ToList();

                        found.AddRange(properties);
                    }

                    if (found.Count == 1)
                    {
                        return found.Single();
                    }
                    else
                    {
                        logger.LogWarning("Expected exactly one connection type, but found multiple: {ConnectionTypes}",
                            string.Join(", ", found.Select(e => e.GetType().FullName)));
                    }
                }
            }

            return new TOptions();
        }
    }
}
