using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationChannelBuilder : INotificationChannelBuilder
{
    public string ChannelType { get; }

    public IServiceCollection Services { get; }

    public NotificationChannelBuilder(
        string channelType,
        IServiceCollection services)
    {
        ChannelType = channelType;
        Services = services;
    }
}
