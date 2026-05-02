using Microsoft.Extensions.Configuration;

namespace EtherGizmos.Common.Providers;

/// <summary>
/// Expands abbreviated connections in a fairly opinionated manner.
/// </summary>
internal class ModularConfigurationConfigurationSource : IConfigurationSource
{
    /// <inheritdoc/>
    public IConfigurationRoot Configuration { get; set; } = null!;

    /// <inheritdoc/>
    public IConfigurationProvider Build(
        IConfigurationBuilder builder)
    {
        return new ModularConfigurationConfigurationProvider(Configuration);
    }
}
