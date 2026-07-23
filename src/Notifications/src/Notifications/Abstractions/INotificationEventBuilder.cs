using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationEventBuilder<TModel>
    where TModel : class
{
    string EventType { get; }

    Type ConfigType { get; }

    IServiceCollection Services { get; }
    string ConfigSchema { get; }
}
