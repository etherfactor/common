using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EtherGizmos.Common.Services;

internal class DomainEventMessageConsumer : IMessageConsumer<DomainEventMessage>
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IDomainEventSerializer _serializer;

    public DomainEventMessageConsumer(
        IServiceProvider serviceProvider,
        IUnitOfWorkFactory uowFactory,
        IDomainEventSerializer serializer)
    {
        _serviceProvider = serviceProvider;
        _uowFactory = uowFactory;
        _serializer = serializer;
    }

    public async Task ConsumeAsync(
        IMessageContext<DomainEventMessage> context)
    {
        var message = context.Message;
        var @event = _serializer.Deserialize(message.PayloadType, message.Payload);

        await (Task)typeof(DomainEventMessageConsumer)
            .GetMethod(nameof(ConsumeInnerAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
            .MakeGenericMethod(@event.GetType())
            .Invoke(this, [message, @event, context.CancellationToken])!;
    }

    private async Task ConsumeInnerAsync<TEvent>(
        DomainEventMessage message,
        TEvent @event,
        CancellationToken cancellationToken = default)
        where TEvent : IDomainEvent
    {
        using var uow = _uowFactory.Create();
        var subscriptionRepo = uow.Repository<NotificationSubscription>();
        var notificationRepo = uow.Repository<Notification>();

        var router = _serviceProvider.GetRequiredService<IDomainEventRouter<TEvent>>();

        var ofType = subscriptionRepo.Data.Where(e => e.IsEnabled && e.EventType == message.EventType);
        var ofTypeFiltered = router.FilterScope(uow, ofType, @event);

        var subscriptions = await ofTypeFiltered.ToListAsync(cancellationToken: cancellationToken);
        foreach (var subscription in subscriptions)
        {
            var notification = new Notification()
            {
                NotificationSubscriptionId = subscription.Id,
                Payload = message.Payload,
                StatusType = NotificationStatusType.Pending,
                AttemptCount = 0,
            };

            notificationRepo.Add(notification);
        }

        await uow.SaveChangesAsync(cancellationToken);
    }
}
