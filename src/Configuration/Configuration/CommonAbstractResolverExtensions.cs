using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common;

public static class CommonAbstractResolverExtensions
{
    extension<TOptions>(IAbstractResolver<TOptions> @this)
        where TOptions : AbstractOptions, new()
    {
        public TSubOptions GetOptions<TSubOptions>(
            string optionsId,
            string expectedType)
            where TSubOptions : class, new()
        {
            var options = @this.Options;
            var logger = @this.ServiceProvider.GetService<ILogger<IAbstractResolver<TOptions>>>()
                ?? NullLogger<IAbstractResolver<TOptions>>.Instance;

            if (options.TryGetValue(optionsId, out var connection))
            {
                if (connection.Type == expectedType)
                {
                    var configuration = @this.ServiceProvider
                        .GetRequiredService<IConfiguration>();

                    var section = configuration.GetSection($"{@this.SectionName}:{optionsId}");

                    var configuredTypes = @this.ServiceProvider
                        .GetRequiredService<IOptionsMonitor<AbstractTypeOptions>>()
                        .CurrentValue;

                    var found = new List<TSubOptions>();
                    if (configuredTypes.ConnectionMap.TryGetValue(typeof(TOptions), out var set))
                    {
                        foreach (var type in set)
                        {
                            var sectionAsType = section.Get(type)!;

                            var properties = sectionAsType.GetType().GetProperties()
                                .Where(e => e.PropertyType.IsAssignableTo(typeof(TSubOptions)))
                                .Select(e => (TSubOptions)e.GetValue(sectionAsType)!)
                                .Where(e => e is not null)
                                .ToList();

                            found.AddRange(properties);
                        }
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

            return new TSubOptions();
        }
    }
}
