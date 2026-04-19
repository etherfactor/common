using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EtherGizmos.Common.Services;

internal class MigrationBuilder : IMigrationBuilder
{
    public string MigrationId { get; }

    public IEnumerable<Assembly> Assemblies { get; }

    public IServiceCollection Services { get; }

    public MigrationBuilder(
        string migrationId,
        IEnumerable<Assembly> assemblies,
        IServiceCollection services)
    {
        MigrationId = migrationId;
        Assemblies = assemblies;
        Services = services;
    }
}
