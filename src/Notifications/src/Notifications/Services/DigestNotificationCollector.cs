using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NCrontab;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

internal class DigestNotificationCollector : NotificationCollector
{
    private readonly ILogger _logger;
    private readonly IUnitOfWorkFactory _uowFactory;
    private readonly IDomainEventSerializer _serializer;
    private readonly IDomainEventEmitter _emitter;

    private readonly ConcurrentDictionary<Type, MethodInfo> _collectBatchInnerLookup = [];

    public override TimeSpan Delay =>
        TimeSpan.FromSeconds(60 - (DateTimeOffset.UtcNow.Second % 60));

    public DigestNotificationCollector(
        ILogger<DigestNotificationCollector> logger,
        IUnitOfWorkFactory uowFactory,
        IDomainEventSerializer serializer,
        IDomainEventEmitter emitter)
        : base(logger)
    {
        _logger = logger;
        _uowFactory = uowFactory;
        _serializer = serializer;
        _emitter = emitter;
    }

    protected override async Task CollectBatchAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        using var uow = _uowFactory.Create();
        var subscriptionRepo = uow.Repository<NotificationSubscription>();
        var notificationRepo = uow.Repository<Notification>();

        var digest = NotificationSchedules.Digest.Id;
        var subscriptions = await subscriptionRepo.Data
            .Where(e => e.ScheduleId == digest
                && (e.NextNotificationAt ?? DateTimeOffset.MinValue) <= DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken: cancellationToken);

        foreach (var subscription in subscriptions)
        {
            using var activity = ActivitySources.Notifications.StartActivity(
                $"Pack notifications into digest",
                ActivityKind.Consumer);

            activity?.SetTag("notification.schedule", NotificationSchedules.Immediate.Id);
            activity?.SetTag("notification.subscription.id", subscription.Id);
            activity?.SetTag("notification.channel", subscription.ChannelId);

            try
            {
                var notifications = await notificationRepo.Data
                    .Where(e => e.SubscriptionId == subscription.Id
                        && !e.IsDerived
                        && e.Status == NotificationStatusType.Pending
                        && e.CreatedAt <= subscription.NextNotificationAt
                        && e.AttemptCount < 10)
                    .ToListAsync(cancellationToken: cancellationToken);

                activity?.SetTag("notification.digest.source_count", notifications.Count);

                var events = new List<object>();
                foreach (var notification in notifications)
                {
                    var link = ActivityContextPropagator.UnpackContext(notification.Headers.AsReadOnly());
                    activity?.AddLink(new(link));

                    var @event = _serializer.Deserialize(notification.PayloadType, notification.Payload);
                    events.Add(@event);
                }

                var configStr = JsonSerializer.Serialize(subscription.ScheduleConfig, JsonSerializerOptions.Web);

                var config = JsonSerializer.Deserialize<DigestScheduleConfig>(configStr, JsonSerializerOptions.Web)!;

                var schedule = CrontabSchedule.Parse(config.CronExpression, new() { IncludingSeconds = false });

                var previous = subscription.LastNotificationAt ?? DateTimeOffset.MinValue;
                var current = subscription.NextNotificationAt ?? DateTimeOffset.UtcNow;

                var scheduleFrom = current < now ? now : current;
                var next = schedule.GetNextOccurrence(scheduleFrom.DateTime);

                var types = events.GroupBy(e => e.GetType());
                foreach (var type in types)
                {
                    //This is really a no-op, just makes it a bit more obvious the IGrouping<Type, object> is also an IEnumerable<object>
                    var typedEvents = type.Select(e => e);

                    var method = _collectBatchInnerLookup.GetOrAdd(type.Key, type =>
                        typeof(DigestNotificationCollector)
                            .GetMethod(nameof(CollectBatchInnerAsync), BindingFlags.Instance | BindingFlags.NonPublic)!
                            .MakeGenericMethod(type));

                    await (Task)method.Invoke(this,
                        [subscription.UserId, previous, current, typedEvents, cancellationToken])!;
                }

                foreach (var notification in notifications)
                {
                    notification.AttemptCount++;
                    notification.Status = NotificationStatusType.Sent;
                    notification.SentAt = DateTimeOffset.UtcNow;
                }

                subscription.LastNotificationAt = subscription.NextNotificationAt ?? current;
                subscription.NextNotificationAt = new DateTimeOffset(next, TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to collect digest notifications for subscription {NotificationSubscriptionId}",
                    subscription.Id);
            }
        }

        await uow.SaveChangesAsync(cancellationToken);
    }

    private async Task CollectBatchInnerAsync<TEvent>(
        string userId,
        DateTimeOffset startAt,
        DateTimeOffset endAt,
        IEnumerable<object> events,
        CancellationToken cancellationToken = default)
        where TEvent : class, IDomainEvent
    {
        var typedEvents = events.Select(e => (TEvent)e).ToList();
        var digest = new Digest<TEvent>()
        {
            StartAt = startAt,
            EndAt = endAt,
            Notifications = typedEvents,
        };

        await _emitter.EmitAsync(
            digest,
            audience: new("$self", userId),
            options: new()
            {
                IsDerived = true,
            },
            cancellationToken: cancellationToken);
    }
}
