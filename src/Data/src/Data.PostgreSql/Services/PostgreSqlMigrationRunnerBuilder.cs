using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using FluentMigrator.Runner;
using System.Reflection;

namespace EtherGizmos.Common.Services;

internal class PostgreSqlMigrationRunnerBuilder : IMigrationRunnerBuilder<PostgreSqlOptions>
{
    public void ConfigureRunner(
        IMigrationRunnerBuilder builder,
        PostgreSqlOptions options,
        IEnumerable<Assembly> assemblies)
    {
        foreach (var assembly in assemblies)
        {
            builder.ScanIn(assembly).For.Migrations()
                .WithVersionTable(new PostgresVersionTableMetadata());
        }

        builder.AddPostgres()
            .WithGlobalConnectionString(options.ConnectionString);
    }
}
