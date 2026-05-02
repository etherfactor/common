using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

internal class DigestNotificationCollectorTests
{
    private Mock<ILogger<DigestNotificationCollector>> _logger = null!;
    private Mock<IUnitOfWorkFactory> _uowFactory = null!;
    private Mock<IUnitOfWork> _uow = null!;
    private Mock<IRepository<NotificationSubscription>> _subscriptionRepository = null!;
    private Mock<IRepository<Notification>> _notificationRepository = null!;
    private Mock<IDomainEventSerializer> _serializer = null!;
    private Mock<IDomainEventEmitter> _emitter = null!;
    private Lazy<TestableDigestNotificationCollector> _collector;

    [SetUp]
    public void SetUp()
    {
        _logger = new Mock<ILogger<DigestNotificationCollector>>();
        _uowFactory = new Mock<IUnitOfWorkFactory>();
        _uow = new Mock<IUnitOfWork>();
        _subscriptionRepository = new Mock<IRepository<NotificationSubscription>>();
        _notificationRepository = new Mock<IRepository<Notification>>();
        _serializer = new Mock<IDomainEventSerializer>();
        _emitter = new Mock<IDomainEventEmitter>();

        _uowFactory
            .Setup(e => e.Create())
            .Returns(_uow.Object);

        _uow
            .Setup(e => e.Repository<NotificationSubscription>())
            .Returns(_subscriptionRepository.Object);

        _uow
            .Setup(e => e.Repository<Notification>())
            .Returns(_notificationRepository.Object);

        _collector = new(() => new(
            _logger.Object,
            _uowFactory.Object,
            _serializer.Object,
            _emitter.Object));
    }

    [Test]
    public void Delay_WhenRead_ShouldReturnValueBetweenOneAndSixtySeconds()
    {
        //Arrange
        var collector = _collector.Value;

        //Act
        var result = collector.Delay;

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.GreaterThan(TimeSpan.Zero));
            Assert.That(result, Is.LessThanOrEqualTo(TimeSpan.FromSeconds(60)));
        }
    }

    [Test]
    public async Task CollectBatchAsync_WhenDigestSubscriptionHasPendingNotifications_ShouldEmitDerivedDigestAndMarkNotificationsSent()
    {
        //Arrange
        var lastNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-10);
        var nextNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var subscription = new NotificationSubscription
        {
            Id = 123,
            UserId = "user-1",
            ScheduleType = NotificationSchedules.Digest.Key,
            ScheduleConfigRaw = JsonSerializer.Serialize(new DigestScheduleConfig
            {
                CronExpression = "* * * * *",
            }, JsonSerializerOptions.Web),
            LastNotificationAt = lastNotificationAt,
            NextNotificationAt = nextNotificationAt,
        };

        var notification1 = new Notification
        {
            Id = 1,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(-30),
            IsDerived = false,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "{\"value\":\"a\"}",
        };

        var notification2 = new Notification
        {
            Id = 2,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(-10),
            IsDerived = false,
            Status = NotificationStatusType.Pending,
            AttemptCount = 2,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "{\"value\":\"b\"}",
        };

        SetupRepositories(
            [subscription],
            [notification1, notification2]);

        _serializer
            .Setup(e => e.Deserialize(notification1.PayloadType, notification1.Payload))
            .Returns(new TestEventA { Value = "a" });

        _serializer
            .Setup(e => e.Deserialize(notification2.PayloadType, notification2.Payload))
            .Returns(new TestEventA { Value = "b" });

        var before = DateTimeOffset.UtcNow;

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();
        var after = DateTimeOffset.UtcNow;

        //Assert
        _emitter.Verify(e => e.EmitAsync(
                It.Is<Digest<TestEventA>>(d =>
                    d.StartAt == lastNotificationAt
                    && d.EndAt == nextNotificationAt
                    && d.Notifications.Count == 2
                    && d.Notifications.Any(x => x.Value == "a")
                    && d.Notifications.Any(x => x.Value == "b")),
                It.Is<IEnumerable<AudienceKey>>(a =>
                    a.Count() == 1
                    && a.Single().Kind == "$self"
                    && a.Single().Id == subscription.UserId),
                It.Is<DomainEventEmissionOptions>(o => o.IsDerived),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Multiple(() =>
        {
            Assert.That(notification1.AttemptCount, Is.EqualTo(1));
            Assert.That(notification2.AttemptCount, Is.EqualTo(3));

            Assert.That(notification1.Status, Is.EqualTo(NotificationStatusType.Sent));
            Assert.That(notification2.Status, Is.EqualTo(NotificationStatusType.Sent));

            Assert.That(notification1.SentAt, Is.Not.Null);
            Assert.That(notification2.SentAt, Is.Not.Null);

            Assert.That(notification1.SentAt, Is.InRange(before, after));
            Assert.That(notification2.SentAt, Is.InRange(before, after));

            Assert.That(subscription.LastNotificationAt, Is.EqualTo(nextNotificationAt));
            Assert.That(subscription.NextNotificationAt, Is.GreaterThan(nextNotificationAt));
        });

        _uow.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CollectBatchAsync_WhenNotificationsContainDifferentEventTypes_ShouldEmitOneDigestPerType()
    {
        //Arrange
        var nextNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var subscription = new NotificationSubscription
        {
            Id = 456,
            UserId = "user-2",
            ScheduleType = NotificationSchedules.Digest.Key,
            ScheduleConfigRaw = JsonSerializer.Serialize(new DigestScheduleConfig
            {
                CronExpression = "* * * * *",
            }, JsonSerializerOptions.Web),
            LastNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-15),
            NextNotificationAt = nextNotificationAt,
        };

        var notification1 = new Notification
        {
            Id = 1,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(-40),
            IsDerived = false,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "payload-a",
        };

        var notification2 = new Notification
        {
            Id = 2,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(-20),
            IsDerived = false,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = typeof(TestEventB).AssemblyQualifiedName!,
            Payload = "payload-b",
        };

        SetupRepositories(
            [subscription],
            [notification1, notification2]);

        _serializer
            .Setup(e => e.Deserialize(notification1.PayloadType, notification1.Payload))
            .Returns(new TestEventA { Value = "a" });

        _serializer
            .Setup(e => e.Deserialize(notification2.PayloadType, notification2.Payload))
            .Returns(new TestEventB { Number = 42 });

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();

        //Assert
        _emitter.Verify(e => e.EmitAsync(
                It.Is<Digest<TestEventA>>(d =>
                    d.Notifications.Count == 1
                    && d.Notifications[0].Value == "a"),
                It.IsAny<IEnumerable<AudienceKey>>(),
                It.Is<DomainEventEmissionOptions>(o => o.IsDerived),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _emitter.Verify(e => e.EmitAsync(
                It.Is<Digest<TestEventB>>(d =>
                    d.Notifications.Count == 1
                    && d.Notifications[0].Number == 42),
                It.IsAny<IEnumerable<AudienceKey>>(),
                It.Is<DomainEventEmissionOptions>(o => o.IsDerived),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _uow.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CollectBatchAsync_WhenNotificationsAreNotEligible_ShouldNotEmitDigestOrMutateNotifications()
    {
        //Arrange
        var nextNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        var subscription = new NotificationSubscription
        {
            Id = 789,
            UserId = "user-3",
            ScheduleType = NotificationSchedules.Digest.Key,
            ScheduleConfigRaw = JsonSerializer.Serialize(new DigestScheduleConfig
            {
                CronExpression = "* * * * *",
            }, JsonSerializerOptions.Web),
            LastNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            NextNotificationAt = nextNotificationAt,
        };

        var ineligibleDerived = new Notification
        {
            Id = 1,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(-10),
            IsDerived = true,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "payload-1",
        };

        var ineligibleFailed = new Notification
        {
            Id = 2,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(-10),
            IsDerived = false,
            Status = NotificationStatusType.Failed,
            AttemptCount = 0,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "payload-2",
        };

        var ineligibleTooManyAttempts = new Notification
        {
            Id = 3,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(-10),
            IsDerived = false,
            Status = NotificationStatusType.Pending,
            AttemptCount = 10,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "payload-3",
        };

        var ineligibleFutureCreatedAt = new Notification
        {
            Id = 4,
            NotificationSubscriptionId = subscription.Id,
            CreatedAt = nextNotificationAt.AddSeconds(10),
            IsDerived = false,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "payload-4",
        };

        SetupRepositories(
            [subscription],
            [ineligibleDerived, ineligibleFailed, ineligibleTooManyAttempts, ineligibleFutureCreatedAt]);

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();

        //Assert
        _serializer.Verify(
            e => e.Deserialize(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);

        _emitter.Verify(
            e => e.EmitAsync(
                It.IsAny<IDomainEvent>(),
                It.IsAny<IEnumerable<AudienceKey>>(),
                It.IsAny<DomainEventEmissionOptions?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        Assert.Multiple(() =>
        {
            Assert.That(ineligibleDerived.AttemptCount, Is.EqualTo(0));
            Assert.That(ineligibleFailed.AttemptCount, Is.EqualTo(0));
            Assert.That(ineligibleTooManyAttempts.AttemptCount, Is.EqualTo(10));
            Assert.That(ineligibleFutureCreatedAt.AttemptCount, Is.EqualTo(0));

            Assert.That(ineligibleDerived.Status, Is.EqualTo(NotificationStatusType.Pending));
            Assert.That(ineligibleFailed.Status, Is.EqualTo(NotificationStatusType.Failed));
            Assert.That(ineligibleTooManyAttempts.Status, Is.EqualTo(NotificationStatusType.Pending));
            Assert.That(ineligibleFutureCreatedAt.Status, Is.EqualTo(NotificationStatusType.Pending));
        });

        _uow.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task CollectBatchAsync_WhenScheduleConfigIsNull_ShouldLogErrorAndContinueAndSaveChanges()
    {
        //Arrange
        var badSubscription = new NotificationSubscription
        {
            Id = 100,
            UserId = "bad-user",
            ScheduleType = NotificationSchedules.Digest.Key,
            ScheduleConfigRaw = null!,
            LastNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            NextNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };

        var goodSubscription = new NotificationSubscription
        {
            Id = 200,
            UserId = "good-user",
            ScheduleType = NotificationSchedules.Digest.Key,
            ScheduleConfigRaw = JsonSerializer.Serialize(new DigestScheduleConfig
            {
                CronExpression = "* * * * *",
            }, JsonSerializerOptions.Web),
            LastNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            NextNotificationAt = DateTimeOffset.UtcNow.AddMinutes(-1),
        };

        var goodNotification = new Notification
        {
            Id = 1,
            NotificationSubscriptionId = goodSubscription.Id,
            CreatedAt = goodSubscription.NextNotificationAt!.Value.AddSeconds(-1),
            IsDerived = false,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = typeof(TestEventA).AssemblyQualifiedName!,
            Payload = "payload-good",
        };

        SetupRepositories(
            [badSubscription, goodSubscription],
            [goodNotification]);

        _serializer
            .Setup(e => e.Deserialize(goodNotification.PayloadType, goodNotification.Payload))
            .Returns(new TestEventA { Value = "good" });

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();

        //Assert
        VerifyLoggedError(
            _logger,
            LogLevel.Error,
            "Failed to collect digest notifications for subscription");

        _emitter.Verify(e => e.EmitAsync(
                It.Is<Digest<TestEventA>>(d =>
                    d.Notifications.Count == 1
                    && d.Notifications[0].Value == "good"),
                It.Is<IEnumerable<AudienceKey>>(a => a.Single().Id == goodSubscription.UserId),
                It.Is<DomainEventEmissionOptions>(o => o.IsDerived),
                It.IsAny<CancellationToken>()),
            Times.Once);

        _uow.Verify(e => e.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupRepositories(
        IEnumerable<NotificationSubscription> subscriptions,
        IEnumerable<Notification> notifications)
    {
        var subscriptionQueryable = subscriptions.ToList().BuildMock();
        var notificationQueryable = notifications.ToList().BuildMock();

        _subscriptionRepository
            .SetupGet(e => e.Data)
            .Returns(subscriptionQueryable);

        _notificationRepository
            .SetupGet(e => e.Data)
            .Returns(notificationQueryable);
    }

    private static void VerifyLoggedError(
        Mock<ILogger<DigestNotificationCollector>> logger,
        LogLevel logLevel,
        string containsMessage)
    {
        logger.Verify(
            x => x.Log(
                logLevel,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(containsMessage, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private sealed class TestableDigestNotificationCollector : DigestNotificationCollector
    {
        public TestableDigestNotificationCollector(
            ILogger<DigestNotificationCollector> logger,
            IUnitOfWorkFactory uowFactory,
            IDomainEventSerializer serializer,
            IDomainEventEmitter emitter)
            : base(logger, uowFactory, serializer, emitter)
        {
        }

        public Task InvokeCollectBatchAsync(CancellationToken cancellationToken = default)
            => CollectBatchAsync(cancellationToken);
    }

    private sealed class TestEventA : IDomainEvent
    {
        public string Value { get; set; } = null!;
    }

    private sealed class TestEventB : IDomainEvent
    {
        public int Number { get; set; }
    }
}
