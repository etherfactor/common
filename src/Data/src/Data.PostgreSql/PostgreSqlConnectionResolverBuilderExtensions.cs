using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class PostgreSqlConnectionResolverBuilderExtensions
{
    extension(IConnectionResolverBuilder @this)
    {
        public IConnectionResolverBuilder WithPostgreSql()
        {
            @this.Services.TryAddSingleton<IDbConnectionFactory<PostgreSqlOptions>, PostgreSqlDbConnectionFactory>();
            @this.Services.TryAddSingleton<IDbContextBuilder<PostgreSqlOptions>, PostgreSqlDbContextBuilder>();
            @this.Services.TryAddSingleton<IMigrationRunnerBuilder<PostgreSqlOptions>, PostgreSqlMigrationRunnerBuilder>();

            ModularConfigurationTypeRegistry.Register<RootPostgreSqlOptions, DatabaseConnectionOptions>(
                sectionName: "Connections",
                itemIdName: "ConnectionId",
                typeName: ConnectionType.Database);

            return @this;
        }
    }
}
