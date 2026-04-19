using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationCreatedMessageConsumer : IMessageConsumer<NotificationCreatedMessage>
{
    public IServiceProvider _serviceProvider;

    public NotificationCreatedMessageConsumer(
        IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task ConsumeAsync(
        IMessageContext<NotificationCreatedMessage> context)
    {
        var message = context.Message;
        var handler = _serviceProvider.GetRequiredKeyedService<INotificationHandler>(message.ScheduleType);

        await handler.HandleAsync(message.NotificationId, context.CancellationToken);
    }
}
