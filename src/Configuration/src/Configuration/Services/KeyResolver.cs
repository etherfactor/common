using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;

namespace EtherGizmos.Common.Services;

internal class KeyResolver : IKeyResolver
{
    private readonly IConfiguration _configuration;

    public IServiceProvider ServiceProvider { get; }

    public string SectionName => "Keys";

    public Dictionary<string, KeyOptions> Options
        => _configuration
            .GetSection(SectionName)
            .Get<Dictionary<string, KeyOptions>>()
                ?? [];

    public KeyResolver(
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        ServiceProvider = serviceProvider;
        _configuration = configuration;
    }
}
