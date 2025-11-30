using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Configuration;

namespace EtherGizmos.Common.Configuration;

internal class ConnectionResolver : IConnectionResolver
{
    private readonly IConfiguration _configuration;

    public IServiceProvider ServiceProvider { get; }

    public Dictionary<string, ConnectionOptions> Options
        => _configuration
            .GetSection("Connections")
            .Get<Dictionary<string, ConnectionOptions>>()
                ?? [];

    public ConnectionResolver(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        ServiceProvider = serviceProvider;
        _configuration = configuration;
    }
}
