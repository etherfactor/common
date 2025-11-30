using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Npgsql;
using System.Data.Common;

namespace EtherGizmos.Common.Services;

internal class PostgreSqlDbConnectionFactory : IConnectionDbConnectionFactory<PostgreSqlOptions>
{
    public DbConnection Create(
        PostgreSqlOptions options)
        => new NpgsqlConnection(options.ConnectionString);
}
