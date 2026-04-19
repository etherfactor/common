using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal;

namespace EtherGizmos.Common.Services;

internal class PostgreSqlDbContextBuilder : IDbContextBuilder<PostgreSqlOptions>
{
    public void ConfigureContext(
        DbContextOptionsBuilder builder,
        PostgreSqlOptions options,
        Action<IRelationalEfOptions>? configureOptions)
    {
        builder.UseNpgsql(
            options.ConnectionString,
            opt =>
            {
                var rel = new RelationalEfOptions<NpgsqlDbContextOptionsBuilder, NpgsqlOptionsExtension>(opt);
                configureOptions?.Invoke(rel);
            });
    }
}
