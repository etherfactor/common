using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace EtherGizmos.Common.Providers;

/// <summary>
/// Expands abbreviated connections in a fairly opinionated manner.
/// </summary>
internal class ModularConfigurationConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationRoot _configuration;

    public ModularConfigurationConfigurationProvider(
        IConfigurationRoot configuration)
    {
        _configuration = new ConfigurationRoot([.. configuration.Providers]);

        //Monitor for changes in the underlying configuration
        ChangeToken.OnChange(
            () => _configuration.GetReloadToken(),
            () =>
            {
                //Reload settings, then mark them as reloaded
                Load();
                OnReload();
            });
    }

    /// <inheritdoc/>
    public override void Load()
    {
        Data.Clear();

        var values = _configuration
            .AsEnumerable()
            .ToList();

        var groups = ModularConfigurationTypeRegistry.Registrations
            .GroupBy(e => new { e.BaseType, e.SectionName, e.ItemIdName, e.TypeName });

        foreach (var group in groups)
        {
            var section = group.Key.SectionName;
            var idname = group.Key.ItemIdName;
            var typename = group.Key.TypeName;
            var properties = group.SelectMany(e => e.Properties);

            foreach (var property in properties)
            {
                var marker = $":{property.Name}:";

                var matches = values
                    .Where(e => e.Key.Contains(marker, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                var prefixes = matches
                    .Select(e => e.Key.Substring(0, e.Key.IndexOf(marker, StringComparison.OrdinalIgnoreCase)))
                    .Distinct();

                foreach (var prefix in prefixes)
                {
                    var id = Guid.NewGuid().ToString();
                    Data[$"{prefix}:{idname}"] = id;
                    Data[$"{section}:{id}:Type"] = typename;

                    foreach (var match in matches.Where(e => e.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                    {
                        var newKey = match.Key.Substring(prefix.Length);
                        Data[$"{section}:{id}{newKey}"] = match.Value;
                    }
                }
            }
        }
    }
}
