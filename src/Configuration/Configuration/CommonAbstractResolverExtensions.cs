using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace EtherGizmos.Common;

public static class CommonAbstractResolverExtensions
{
    extension<TRoot>(IAbstractResolver<TRoot> @this)
        where TRoot : AbstractOptions, new()
    {
        public TBase GetOptions<TBase>(
            string optionsId,
            string expectedType)
            where TBase : class
        {
            var options = @this.Options;
            var logger = @this.ServiceProvider.GetService<ILogger<IAbstractResolver<TRoot>>>()
                ?? NullLogger<IAbstractResolver<TRoot>>.Instance;

            if (!options.TryGetValue(optionsId, out var entry))
            {
                throw new InvalidOperationException(
                    $"No entry is configured with id '{optionsId}' in section '{@this.SectionName}'.");
            }

            if (!string.Equals(entry.Type, expectedType, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"Entry '{optionsId}' is of type '{entry.Type}', expected '{expectedType}'.");
            }

            var configuration = @this.ServiceProvider
                .GetRequiredService<IConfiguration>();

            var registrations = AbstractTypeRegistry.Registrations
                .Where(r =>
                    r.RootType.IsAssignableTo(typeof(TRoot)) &&
                    r.BaseType.IsAssignableFrom(typeof(TBase)))
                .ToList();

            var found = new List<TBase>();

            foreach (var registration in registrations)
            {
                var section = configuration.GetSection($"{registration.SectionName}:{optionsId}");

                var sectionAsType = section.Get(registration.RootType);
                if (sectionAsType is null)
                    continue;

                foreach (var property in registration.Properties)
                {
                    if (!typeof(TBase).IsAssignableFrom(property.PropertyType))
                        continue;

                    var value = property.GetValue(sectionAsType);
                    if (value is TBase typed)
                    {
                        found.Add(typed);
                    }
                }
            }

            if (found.Count == 1)
            {
                return found[0];
            }

            if (found.Count == 0)
            {
                throw new InvalidOperationException(
                    $"Entry '{optionsId}' did not have any nested configuration matching '{typeof(TBase).FullName}'.");
            }

            logger.LogWarning(
                "Expected exactly one nested options object for '{OptionsId}', but found multiple: {Types}",
                optionsId,
                string.Join(", ", found.Select(e => e.GetType().FullName)));

            throw new InvalidOperationException(
                $"Entry '{optionsId}' has ambiguous nested configuration for '{typeof(TBase).FullName}'.");
        }
    }
}
