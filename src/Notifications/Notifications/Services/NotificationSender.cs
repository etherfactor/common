using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationSender : INotificationSender
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IDomainEventSerializer _serializer;

    public NotificationSender(
        IServiceProvider serviceProvider,
        IDomainEventSerializer serializer)
    {
        _serviceProvider = serviceProvider;
        _serializer = serializer;
    }

    public async Task SendAsync(
        Notification notification,
        CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var provider = scope.ServiceProvider;

        var channelKey = notification.NotificationSubscription.ChannelKey;

        var model = _serializer.Deserialize(notification.PayloadType, notification.Payload);

        var formatter = provider.GetRequiredKeyedService<INotificationChannelFormatter>((channelKey, model.GetType()));
        var sender = provider.GetRequiredKeyedService<INotificationChannelSender>(channelKey);

        var envelope = formatter.Format(notification, model);
        await sender.SendAsync(envelope, cancellationToken);
    }
}
