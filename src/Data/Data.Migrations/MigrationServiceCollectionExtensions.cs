using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using FluentMigrator.Runner;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EtherGizmos.Common;

public static class MigrationServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        public IMigrationBuilder AddMigrations(
            params IEnumerable<Assembly> assemblies)
            => @this.AddMigrations(string.Empty, assemblies);

        public IMigrationBuilder AddMigrations(
            string migrationId,
            params IEnumerable<Assembly> assemblies)
        {
            return new MigrationBuilder(migrationId, assemblies, @this);
        }
    }

    extension(IMigrationBuilder @this)
    {
        public IMigrationBuilder UseConnection(
            string connectionId)
        {
            @this.Services
                .AddChildContainer((child, parent) =>
                {
                    var resolver = parent.GetRequiredService<IConnectionResolver>();
                    var connection = resolver.GetDatabaseConnection(connectionId);
                    var type = connection.GetType();

                    child.AddFluentMigratorCore()
                        .ConfigureRunner(opt =>
                        {
                            typeof(MigrationServiceCollectionExtensions)
                                .GetMethod(nameof(InnerUseConnection), BindingFlags.NonPublic | BindingFlags.Static)!
                                .MakeGenericMethod([type])
                                .Invoke(null, [@this, parent, opt, connection, @this.Assemblies]);
                        });
                })
                .ImportLogging()
                .ForwardScoped<IMigrationRunner>();

            return @this;
        }

        internal void InnerUseConnection<TOptions>(
            IServiceProvider provider,
            IMigrationRunnerBuilder builder,
            TOptions options,
            IEnumerable<Assembly> assemblies)
            where TOptions : DatabaseConnectionOptions, new()
        {
            var factory = provider.GetService<IMigrationRunnerBuilder<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for configuring a migration runner for type {typeof(TOptions)}");

            factory.ConfigureRunner(builder, options, assemblies);
        }
    }
}
