using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Abstractions;

public interface INotificationEventBuilder<TModel>
    where TModel : class
{
    string EventType { get; }

    IServiceCollection Services { get; }
}
