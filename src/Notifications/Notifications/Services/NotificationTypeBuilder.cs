using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationTypeBuilder<TModel> : INotificationTypeBuilder<TModel>
    where TModel : class
{
    public string EventType { get; }

    public IServiceCollection Services { get; }

    public NotificationTypeBuilder(
        string eventType,
        IServiceCollection services)
    {
        EventType = eventType;
        Services = services;
    }
}
