using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationChannelBuilder
{
    string ChannelType { get; }

    IServiceCollection Services { get; }
}
