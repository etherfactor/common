using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface IConnectionResolverBuilder
{
    IServiceCollection Services { get; }
}
