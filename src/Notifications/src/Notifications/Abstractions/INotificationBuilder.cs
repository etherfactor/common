using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationBuilder
{
    IServiceCollection Services { get; }
}
