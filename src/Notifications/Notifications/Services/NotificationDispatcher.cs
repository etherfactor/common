using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationDispatcher : INotificationDispatcher
{
    private readonly IServiceProvider _serviceProvider;

    public NotificationDispatcher(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        var channelKey = notification.NotificationSubscription.ChannelKey;

        var formatter = _serviceProvider.GetRequiredKeyedService<INotificationChannelFormatter>(channelKey);
        var sender = _serviceProvider.GetRequiredKeyedService<INotificationChannelSender>(channelKey);

        var envelope = formatter.Format(notification);
        await sender.SendAsync(envelope, cancellationToken);
    }
}
