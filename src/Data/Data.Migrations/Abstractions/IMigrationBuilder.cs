using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EtherGizmos.Common.Abstractions;

public interface IMigrationBuilder
{
    string MigrationId { get; }

    IEnumerable<Assembly> Assemblies { get; }

    IServiceCollection Services { get; }
}
