using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;

namespace EtherGizmos.Common.Services;

internal class ConnectionResolver : IConnectionResolver
{
    private readonly IConfiguration _configuration;

    public IServiceProvider ServiceProvider { get; }

    public string SectionName => "Connections";

    public Dictionary<string, ConnectionOptions> Options
        => _configuration
            .GetSection(SectionName)
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
