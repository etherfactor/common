using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Data.Common;
using System.Reflection;

namespace EtherGizmos.Common;

public static class DatabaseConnectionResolverExtensions
{
    extension(IConnectionResolver @this)
    {
        public DatabaseConnectionOptions GetDatabaseConnection(
            string connectionId)
        {
            var connection = @this.GetOptions<ConnectionOptions, DatabaseConnectionOptions>(connectionId, ConnectionType.Database);
            return connection;
        }

        public DbConnection CreateDbConnection(
            string connectionId)
        {
            var connection = @this.GetDatabaseConnection(connectionId);
            var type = connection.GetType();

            var result = (DbConnection)typeof(DatabaseConnectionResolverExtensions)
                .GetMethod(nameof(InnerCreateDbConnection), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod([type])
                .Invoke(null, [@this, connectionId, connection])!;

            return result;
        }

        internal DbConnection InnerCreateDbConnection<TOptions>(
            string connectionId,
            TOptions options)
            where TOptions : DatabaseConnectionOptions, new()
        {
            var factory = @this.ServiceProvider.GetService<IDbConnectionFactory<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for creating a DbConnection for type {typeof(TOptions)}");

            return factory.Create(options);
        }
    }
}
