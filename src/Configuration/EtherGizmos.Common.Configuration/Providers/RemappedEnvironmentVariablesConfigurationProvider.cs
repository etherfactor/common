using Microsoft.Extensions.Configuration.EnvironmentVariables;
using System.Text.RegularExpressions;

namespace EtherGizmos.Common.Providers;

/// <summary>
/// Remaps environment variable key names, creating additional keys if they do not already exist.
/// </summary>
internal class RemappedEnvironmentVariablesConfigurationProvider : EnvironmentVariablesConfigurationProvider
{
    private readonly IEnumerable<(Regex Match, string Replacement)> _remaps;

    public RemappedEnvironmentVariablesConfigurationProvider(
        IEnumerable<(Regex Match, string Replacement)> remaps)
    {
        _remaps = remaps;
    }

    /// <inheritdoc/>
    public override void Load()
    {
        base.Load();

        var newData = new Dictionary<string, string?>();
        foreach (var datum in Data)
        {
            var key = datum.Key;
            var value = datum.Value;
            newData[key] = value;
            foreach (var remap in _remaps)
            {
                var regex = remap.Match;
                var replacement = remap.Replacement;
                key = regex.Replace(key, replacement);
            }

            newData.TryAdd(key, value);
        }

        Data = newData;
    }
}
