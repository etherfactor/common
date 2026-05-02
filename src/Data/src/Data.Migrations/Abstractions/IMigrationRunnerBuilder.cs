using EtherGizmos.Common.Configuration;
using FluentMigrator.Runner;
using System.Reflection;

namespace EtherGizmos.Common.Abstractions;

public interface IMigrationRunnerBuilder<TOptions>
    where TOptions : DatabaseConnectionOptions
{
    void ConfigureRunner(
        IMigrationRunnerBuilder builder,
        TOptions options,
        IEnumerable<Assembly> assemblies);
}
