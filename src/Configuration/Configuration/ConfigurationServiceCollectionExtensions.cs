using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class ConfigurationServiceCollectionExtensions
{
    extension(IServiceCollection @this)
    {
        public IConnectionResolverBuilder AddConnectionResolver()
        {
            @this.TryAddSingleton<IConnectionResolver, ConnectionResolver>();
            return new ConnectionResolverBuilder(@this);
        }
    }

    private class ConnectionResolverBuilder : IConnectionResolverBuilder
    {
        public IServiceCollection Services { get; }

        public ConnectionResolverBuilder(
            IServiceCollection services)
        {
            Services = services;
        }
    }
}
