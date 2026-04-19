using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface IKeyResolverBuilder
{
    IServiceCollection Services { get; }
}
