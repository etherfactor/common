using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;
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

        //If we route the message to a specific subscription, we can cut in front of the fan-out logic and pick that subscription
        if (message.Audiences.Any(e => "$sub".Equals(e.Kind, StringComparison.OrdinalIgnoreCase)))
        {
            using var uow = _uowFactory.Create();
            var subscriptionRepo = uow.Repository<NotificationSubscription>();
            var notificationRepo = uow.Repository<Notification>();

            var subAudience = message.Audiences
                .First(e => "$sub".Equals(e.Kind, StringComparison.OrdinalIgnoreCase));

            if (!long.TryParse(subAudience.Id, out var subscriptionId))
            {
                _logger.LogWarning("Domain event contained an invalid $sub audience");
                return;
            }

            using var subActivity = ActivitySources.Notifications.StartActivityFromCarrier(
                $"Route domain event to target subscription",
                ActivityKind.Producer,
                context.RawMessage.Headers);

            var subscription = await subscriptionRepo.Data
                .SingleOrDefaultAsync(e => e.Id == subscriptionId && e.IsEnabled, cancellationToken: context.CancellationToken);

            if (subscription is null)
            {
                _logger.LogWarning(
                    "Target subscription {NotificationSubscriptionId} was not found or is disabled",
                    subscriptionId);
                return;
            }

            var notification = new Notification()
            {
                OccurrenceId = message.EventId,
                CreatedAt = DateTimeOffset.UtcNow,
                SubscriptionId = subscription.Id,
                EventId = subscription.EventId,
                ChannelId = subscription.ChannelId,
                ScheduleId = subscription.ScheduleId,
                IsDerived = message.IsDerived,
                PayloadType = message.PayloadType,
                Payload = message.Payload,
                Status = NotificationStatusType.Pending,
                Headers = ActivityContextPropagator.Pack(Activity.Current).ToDictionary(),
                AttemptCount = 0,
            };

            notificationRepo.Add(notification);

            await uow.SaveChangesAsync(context.CancellationToken);

            try
            {
                await _sender.SendAsync(
                    NotificationConstants.NotificationsLogicalName,
                    new NotificationCreatedMessage()
                    {
                        NotificationId = notification.Id,
                        ScheduleType = subscription.ScheduleId,
                    },
                    cancellationToken: context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish created message for notification {NotificationId}",
                    notification.Id);
            }

            return;
        }

        //Otherwise we need to find the correct router and send it to the correct location
        using var activity = ActivitySources.Notifications.StartActivityFromCarrier(
            $"Fan out domain event to subscriptions",
            ActivityKind.Producer,
            context.RawMessage.Headers);

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

        var subscriptions = await subscriptionRepo.Data.Where(e => e.IsEnabled && e.EventId == message.EventType)
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
                    OccurrenceId = message.EventId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    SubscriptionId = subscription.Id,
                    EventId = subscription.EventId,
                    ChannelId = subscription.ChannelId,
                    ScheduleId = subscription.ScheduleId,
                    IsDerived = message.IsDerived,
                    PayloadType = message.PayloadType,
                    Payload = message.Payload,
                    Status = NotificationStatusType.Pending,
                    Headers = ActivityContextPropagator.Pack(Activity.Current).ToDictionary(),
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
                        ScheduleType = entry.Subscription.ScheduleId,
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
