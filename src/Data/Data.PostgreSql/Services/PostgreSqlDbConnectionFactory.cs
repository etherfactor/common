using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Npgsql;
using System.Data.Common;

namespace EtherGizmos.Common.Services;

internal class PostgreSqlDbConnectionFactory : IDbConnectionFactory<PostgreSqlOptions>
{
    public DbConnection Create(
        PostgreSqlOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new NpgsqlConnection(options.ConnectionString);
    }
}
