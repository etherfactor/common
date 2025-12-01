using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;
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

        public IKeyResolverBuilder AddKeyResolver()
        {
            @this.TryAddSingleton<IKeyResolver, KeyResolver>();
            return new KeyResolverBuilder(@this);
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

    private class KeyResolverBuilder : IKeyResolverBuilder
    {
        public IServiceCollection Services { get; }

        public KeyResolverBuilder(
            IServiceCollection services)
        {
            Services = services;
        }
    }
}
