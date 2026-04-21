using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationEventBuilder<TModel> : INotificationEventBuilder<TModel>
    where TModel : class
{
    public string EventType { get; }

    public IServiceCollection Services { get; }

    public NotificationEventBuilder(
        string eventType,
        IServiceCollection services)
    {
        EventType = eventType;
        Services = services;
    }
}
