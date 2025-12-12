using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface IMessagingBuilder
{
    public IServiceCollection Services { get; }
}
