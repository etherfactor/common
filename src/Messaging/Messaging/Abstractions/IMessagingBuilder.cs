using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface IMessagingBuilder
{
    string BusId { get; }

    public IServiceCollection Services { get; }
}
