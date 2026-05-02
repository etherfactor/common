using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationBuilder : INotificationBuilder
{
    public IServiceCollection Services { get; }

    public NotificationBuilder(
        IServiceCollection services)
    {
        Services = services;
    }
}
