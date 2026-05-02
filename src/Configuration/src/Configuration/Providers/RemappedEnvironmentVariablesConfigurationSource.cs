using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace EtherGizmos.Common.Providers;

/// <summary>
/// Remaps environment variable key names, creating additional keys if they do not already exist.
/// </summary>
internal sealed class RemappedEnvironmentVariablesConfigurationSource : IConfigurationSource
{
    public IEnumerable<(Regex Match, string Replacement)> Remaps { get; set; } = [];

    /// <inheritdoc/>
    public IConfigurationProvider Build(
        IConfigurationBuilder builder)
    {
        return new RemappedEnvironmentVariablesConfigurationProvider(Remaps);
    }
}
