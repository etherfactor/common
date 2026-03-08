using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Reflection;

namespace EtherGizmos.Common.Services;

internal class DomainEventMessageConsumer : IMessageConsumer<DomainEventMessage>
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IDomainEventSerializer _serializer;
    private readonly IMessageSender _sender;

    private readonly ConcurrentDictionary<Type, MethodInfo> _consumeInnerLookup = [];

    public DomainEventMessageConsumer(
        ILogger<DomainEventMessageConsumer> logger,
        IServiceProvider serviceProvider,
        IUnitOfWorkFactory uowFactory,
        IDomainEventSerializer serializer,
        IMessageSender sender)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _uowFactory = uowFactory;
        _serializer = serializer;
        _sender = sender;
    }

    public async Task ConsumeAsync(
        IMessageContext<DomainEventMessage> context)
    {
        var message = context.Message;
        var @event = _serializer.Deserialize(message.PayloadType, message.Payload);

        var method = _consumeInnerLookup.GetOrAdd(@event.GetType(), type =>
            typeof(DomainEventMessageConsumer)
                .GetMethod(nameof(ConsumeInnerAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
                .MakeGenericMethod(type));

        await (Task)method.Invoke(this, [message, @event, context.CancellationToken])!;
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

        var subscriptions = await subscriptionRepo.Data.Where(e => e.IsEnabled && e.EventType == message.EventType)
            .ToListAsync(cancellationToken: cancellationToken);
        var subsByUser = subscriptions.ToLookup(e => e.UserId, StringComparer.OrdinalIgnoreCase);

        var allUsers = subscriptions.Select(e => e.UserId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var applicableUsers = router.FilterScopeAsync(@event, message.Audiences, allUsers, cancellationToken);

        var toPublish = new List<(NotificationSubscription Subscription, Notification Notification)>();
        var seenUsers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await foreach (var userId in applicableUsers)
        {
            if (!seenUsers.Add(userId)) continue;

            var userSubs = subsByUser[userId];
            foreach (var subscription in userSubs)
            {
                var notification = new Notification()
                {
                    EventId = message.EventId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    NotificationSubscriptionId = subscription.Id,
                    IsDerived = message.IsDerived,
                    PayloadType = message.PayloadType,
                    Payload = message.Payload,
                    StatusType = NotificationStatusType.Pending,
                    AttemptCount = 0,
                };

                notificationRepo.Add(notification);
                toPublish.Add((subscription, notification));
            }
        }

        await uow.SaveChangesAsync(cancellationToken);

        foreach (var entry in toPublish)
        {
            try
            {
                await _sender.SendAsync(
                    NotificationConstants.NotificationsLogicalName,
                    new NotificationCreatedMessage()
                    {
                        NotificationId = entry.Notification.Id,
                        ScheduleType = entry.Subscription.ScheduleType,
                    },
                    cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish created message for notification {NotificationId}",
                    entry.Notification.Id);
            }
        }
    }
}
