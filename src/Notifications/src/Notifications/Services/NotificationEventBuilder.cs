using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationEventBuilder<TModel> : INotificationEventBuilder<TModel>
    where TModel : class
{
    public string EventType { get; }

    public Type ConfigType { get; }

    public string ConfigSchema { get; }

    public IServiceCollection Services { get; }

    public NotificationEventBuilder(
        string eventType,
        Type configType,
        string configSchema,
        IServiceCollection services)
    {
        EventType = eventType;
        ConfigType = configType;
        ConfigSchema = configSchema;
        Services = services;
    }
}
