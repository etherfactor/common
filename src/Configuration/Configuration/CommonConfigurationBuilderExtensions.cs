using EtherGizmos.Common.Providers;
using Microsoft.Extensions.Configuration;
using System.Text.RegularExpressions;

namespace EtherGizmos.Common;

/// <summary>
/// Provides extension methods for <see cref="IConfigurationBuilder"/>.
/// </summary>
public static class CommonConfigurationBuilderExtensions
{
    extension(IConfigurationBuilder @this)
    {
        /// <summary>
        /// Adds environment variables, but remaps their values based on regular expressions. The matched patterns will be
        /// replaced with the specified value. If the new key name does not exist, it will be created with the current value.
        /// </summary>
        /// <param name="this">Itself.</param>
        /// <param name="remaps">The remaps to apply.</param>
        /// <returns></returns>
        public IConfigurationBuilder AddRemappedEnvironmentVariables(
            params (Regex Remap, string Replacement)[] remaps)
        {
            @this.Add(new RemappedEnvironmentVariablesConfigurationSource()
            {
                Remaps = remaps,
            });

            return @this;
        }

        /// <summary>
        /// References the current state of the configuration to expand abbreviated connections in a fairly opinionated manner.
        /// For example, AppDatabase:PostgreSql:ConnectionString would create a new 'Database' connection, assign it to
        /// AppDatabase:ConnectionId, then copy all of the PostgreSql:[Settings] to the connection.
        /// </summary>
        /// <param name="this">Itself.</param>
        /// <param name="configuration">The current configuration state.</param>
        /// <returns>Itself.</returns>
        public IConfigurationBuilder AddExpandedConnections(
            IConfigurationRoot configuration)
        {
            @this.Add(new ExpandedConnectionsVariablesConfigurationSource()
            {
                Configuration = configuration,
            });

            return @this;
        }
    }
}
