
using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common.Abstractions;

public interface IConnectionResolver
{
    IServiceProvider ServiceProvider { get; }

    Dictionary<string, ConnectionOptions> Options { get; }
}
