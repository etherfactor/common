using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace EtherGizmos.Common.Providers;

/// <summary>
/// Expands abbreviated connections in a fairly opinionated manner.
/// </summary>
internal class ExpandedConnectionsVariablesConfigurationProvider : ConfigurationProvider
{
    private readonly IConfigurationRoot _configuration;

    /// <summary>
    /// The markers that represent a database connection.
    /// </summary>
    private static readonly IEnumerable<string> _databases =
    [
        "PostgreSql",
    ];

    /// <summary>
    /// The markers that represent a certificate.
    /// </summary>
    private static readonly IEnumerable<string> _certificates =
    [
        "File",
        "Text",
    ];

    public ExpandedConnectionsVariablesConfigurationProvider(
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

        foreach (var database in _databases)
        {
            var marker = $":{database}:";

            var matches = values
                .Where(e => e.Key.Contains(marker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var prefixes = matches
                .Select(e => e.Key.Substring(0, e.Key.IndexOf(marker, StringComparison.OrdinalIgnoreCase)))
                .Distinct();

            foreach (var prefix in prefixes)
            {
                var connectionId = Guid.NewGuid().ToString();
                Data[$"{prefix}:ConnectionId"] = connectionId;
                Data[$"Connections:{connectionId}:Type"] = "Database";

                foreach (var match in matches.Where(e => e.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    var newKey = match.Key.Substring(prefix.Length);
                    Data[$"Connections:{connectionId}{newKey}"] = match.Value;
                }
            }
        }

        foreach (var certificate in _certificates)
        {
            var marker = $":{certificate}:";

            var matches = values
                .Where(e => e.Key.Contains(marker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var prefixes = matches
                .Select(e => e.Key.Substring(0, e.Key.IndexOf(marker, StringComparison.OrdinalIgnoreCase)))
                .Distinct();

            foreach (var prefix in prefixes)
            {
                var certificateId = Guid.NewGuid().ToString();
                Data[$"{prefix}:CertificateId"] = certificateId;
                Data[$"Security:Certificates:{certificateId}:Type"] = "Certificate";

                foreach (var match in matches.Where(e => e.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)))
                {
                    var newKey = match.Key.Substring(prefix.Length);
                    Data[$"Security:Certificates:{certificateId}{newKey}"] = match.Value;
                }
            }
        }
    }
}
