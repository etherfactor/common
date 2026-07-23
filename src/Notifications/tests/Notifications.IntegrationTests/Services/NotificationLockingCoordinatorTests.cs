using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Converters;
using EtherGizmos.Common.Extensions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

internal class NotificationLockingCoordinatorTests : IntegrationTestBase
{
    private NotificationLockingCoordinator _coordinator;
    private NotificationContext _context;
    private IServiceProvider _serviceProvider;
    private List<NotificationSubscription> _subscriptions;
    private string _payload;
    private string _payloadType;
    private string _connectionString;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connectionString = await Setup.CreateDatabase("locking_coordinator");

        var services = new ServiceCollection();

        services
            .AddDbContext<NotificationContext>(opt =>
            {
                opt.UseNpgsql(_connectionString)
                    .EnableSensitiveDataLogging();
            });

        services.AddKeyedSingleton("Notification", new Mock<IMigrationManager>().Object);

        using var serviceProvider = services.BuildServiceProvider();

        var scope = serviceProvider.CreateScope();

        using var context = scope.ServiceProvider.GetRequiredService<NotificationContext>();
        await context.Database.EnsureCreatedAsync();

        var @event = new NotificationEvent()
        {
            Id = "test.domain.event",
            Name = "Test Domain Event",
            IsAvailable = true,
            LastSeenAt = DateTimeOffset.UtcNow,
            ConfigSchema = new Dictionary<string, object?>(),
            Supports = [],
        };
        context.NotificationEvents.Add(@event);

        var channel = new NotificationChannel()
        {
            Id = "test.domain.event",
            Name = "Test Domain Event",
            IsAvailable = true,
            LastSeenAt = DateTimeOffset.UtcNow,
            ConfigSchema = new Dictionary<string, object?>(),
        };
        context.NotificationChannels.Add(channel);

        var immSchedule = new NotificationSchedule()
        {
            Id = NotificationSchedules.Immediate.Id,
            Name = "Immediate",
            IsAvailable = true,
            LastSeenAt = DateTimeOffset.UtcNow,
            ConfigSchema = new Dictionary<string, object?>(),
        };
        context.NotificationSchedules.Add(immSchedule);

        var digSchedule = new NotificationSchedule()
        {
            Id = NotificationSchedules.Digest.Id,
            Name = "Digest",
            IsAvailable = true,
            LastSeenAt = DateTimeOffset.UtcNow,
            ConfigSchema = new Dictionary<string, object?>(),
        };
        context.NotificationSchedules.Add(digSchedule);

        var tstChannel = new NotificationChannel()
        {
            Id = "test",
            Name = "Test",
            IsAvailable = true,
            LastSeenAt = DateTimeOffset.UtcNow,
            ConfigSchema = new Dictionary<string, object?>(),
        };
        context.NotificationChannels.Add(tstChannel);

        await context.SaveChangesAsync();
    }

    [SetUp]
    public async Task SetUp()
    {
        var services = new ServiceCollection();

        services
            .AddDbContext<NotificationContext>(opt =>
            {
                opt.UseNpgsql(_connectionString)
                    .EnableSensitiveDataLogging();
            });

        services
            .AddUnitOfWork(opt =>
            {
                opt.BindDbContext<NotificationContext>();
            });

        services.AddSingleton<INotificationLockingCoordinator, NotificationLockingCoordinator>();

        services.AddKeyedSingleton("Notification", new Mock<IMigrationManager>().Object);

        _serviceProvider = services.BuildServiceProvider();

        var scope = _serviceProvider.CreateScope();

        _context = scope.ServiceProvider.GetRequiredService<NotificationContext>();
        await _context.Database.EnsureCreatedAsync();

        _coordinator = (NotificationLockingCoordinator)scope.ServiceProvider.GetRequiredService<INotificationLockingCoordinator>();

        var immediate = new NotificationSubscription
        {
            UserId = "123",
            EventId = "test.domain.event",
            ScheduleId = NotificationSchedules.Immediate.Id,
            ScheduleConfig = new Dictionary<string, object?>(),
            ChannelId = "test",
            ChannelConfig = new Dictionary<string, object?>(),
        };

        var digest = new NotificationSubscription
        {
            UserId = "123",
            EventId = "test.domain.event",
            ScheduleId = NotificationSchedules.Digest.Id,
            ScheduleConfig = new Dictionary<string, object?>(),
            ChannelId = "test",
            ChannelConfig = new Dictionary<string, object?>(),
        };

        _subscriptions =
        [
            immediate,
            digest,
        ];

        var serializer = new DomainEventSerializer();

        var model = new TestDomainEvent();
        var serialized = serializer.Serialize(model);

        _payload = serialized.Payload;
        _payloadType = serialized.Type;

        await _context.SaveChangesAsync();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    public async Task ClaimBatchAsync_WhenPendingNotificationsExist_ShouldClaimUpToMaxCountInIdOrder()
    {
        //Arrange
        var notification1 = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        var notification2 = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        var notification3 = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        var notification4 = new Notification
        {
            SubscriptionId = _subscriptions[1].Id,
            Subscription = _subscriptions[1],
            EventId = _subscriptions[1].EventId,
            ChannelId = _subscriptions[1].ChannelId,
            ScheduleId = _subscriptions[1].ScheduleId,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        _context.Notifications.AddRange(notification1, notification2, notification3, notification4);
        await _context.SaveChangesAsync();

        //Act
        var claims = await _coordinator.ClaimBatchAsync(NotificationSchedules.Immediate, maxCount: 2);

        //Assert
        Assert.That(claims, Has.Count.EqualTo(2));
        Assert.That(claims.Select(e => e.LockId).Distinct().Count(), Is.EqualTo(1));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(claims[0].Notification.Subscription, Is.Not.Null);
            Assert.That(claims[1].Notification.Subscription, Is.Not.Null);
        }

        _context.ChangeTracker.Clear();

        var notificationIds = new long[] { notification1.Id, notification2.Id, notification3.Id, notification4.Id };
        var notifications = await _context.Notifications
            .Where(e => notificationIds.Contains(e.Id))
            .OrderBy(e => e.Id).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(notifications.Single(e => e.Id == notification1.Id).Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(notifications.Single(e => e.Id == notification2.Id).Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(notifications.Single(e => e.Id == notification3.Id).Status, Is.EqualTo(NotificationStatusType.Pending));
            Assert.That(notifications.Single(e => e.Id == notification4.Id).Status, Is.EqualTo(NotificationStatusType.Pending));

            Assert.That(notifications.Single(e => e.Id == notification1.Id).AttemptCount, Is.EqualTo(1));
            Assert.That(notifications.Single(e => e.Id == notification2.Id).AttemptCount, Is.EqualTo(1));
            Assert.That(notifications.Single(e => e.Id == notification3.Id).AttemptCount, Is.EqualTo(0));
            Assert.That(notifications.Single(e => e.Id == notification4.Id).AttemptCount, Is.EqualTo(0));

            Assert.That(notifications.Single(e => e.Id == notification1.Id).LockId, Is.Not.Null);
            Assert.That(notifications.Single(e => e.Id == notification2.Id).LockId, Is.Not.Null);
            Assert.That(notifications.Single(e => e.Id == notification3.Id).LockId, Is.Null);
            Assert.That(notifications.Single(e => e.Id == notification4.Id).LockId, Is.Null);
        }
    }

    [Test]
    public async Task ClaimBatchAsync_WhenExpiredInFlightNotificationExists_ShouldReclaimIt()
    {
        //Arrange
        var expired = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 2,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = Guid.NewGuid(),
            LockedBy = "old-machine",
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(-1),
        };
        _context.Notifications.Add(expired);
        await _context.SaveChangesAsync();

        //Act
        var claims = await _coordinator.ClaimBatchAsync(NotificationSchedules.Immediate);

        //Assert
        Assert.That(claims, Has.Count.EqualTo(1));
        Assert.That(claims[0].NotificationId, Is.EqualTo(expired.Id));

        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == expired.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.AttemptCount, Is.EqualTo(3));
            Assert.That(updated.LockId, Is.EqualTo(claims[0].LockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
            Assert.That(updated.LastAttemptAt, Is.Not.Null);
        }
    }

    [Test]
    public async Task ClaimBatchAsync_WhenNotificationIsInFlightAndNotExpired_ShouldNotClaimIt()
    {
        //Arrange
        var originalLockId = Guid.NewGuid();
        var inflight = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 2,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = originalLockId,
            LockedBy = "busy-machine",
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(5),
        };

        _context.Notifications.Add(inflight);
        await _context.SaveChangesAsync();

        //Act
        var claims = await _coordinator.ClaimBatchAsync(NotificationSchedules.Immediate);

        //Assert
        Assert.That(claims, Has.Count.EqualTo(0));

        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == inflight.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.AttemptCount, Is.EqualTo(2));
            Assert.That(updated.LockId, Is.EqualTo(originalLockId));
            Assert.That(updated.LockedBy, Is.EqualTo("busy-machine"));
        }
    }

    [Test]
    public async Task ClaimBatchAsync_WhenAttemptCountIsTen_ShouldNotClaimIt()
    {
        //Arrange
        var maxattempts = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.Pending,
            AttemptCount = 10,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        _context.Notifications.Add(maxattempts);
        await _context.SaveChangesAsync();

        //Act
        var claims = await _coordinator.ClaimBatchAsync(NotificationSchedules.Immediate);

        //Assert
        Assert.That(claims, Is.Empty);

        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == maxattempts.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Pending));
            Assert.That(updated.AttemptCount, Is.EqualTo(10));
        }
    }

    [Test]
    public async Task ClaimSingleAsync_WhenNotificationCanBeLocked_ShouldReturnClaimAndUpdateNotification()
    {
        //Arrange
        var notification = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        //Act
        var claim = await _coordinator.ClaimSingleAsync(notification.Id);

        //Assert
        Assert.That(claim, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(claim!.NotificationId, Is.EqualTo(notification.Id));
            Assert.That(claim.Notification.Subscription, Is.Not.Null);
        }

        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == notification.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.AttemptCount, Is.EqualTo(1));
            Assert.That(updated.LockId, Is.EqualTo(claim.LockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
            Assert.That(updated.LastAttemptAt, Is.Not.Null);
        }
    }

    [Test]
    public async Task ClaimSingleAsync_WhenNotificationCannotBeLocked_ShouldReturnNull()
    {
        //Arrange
        var inflight = new Notification
        {
            Id = 50,
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 2,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = Guid.NewGuid(),
            LockedBy = "busy-machine",
            LockedUntil = DateTimeOffset.UtcNow.AddMinutes(5),
        };

        _context.Notifications.Add(inflight);
        await _context.SaveChangesAsync();

        //Act
        var claim = await _coordinator.ClaimSingleAsync(50);

        //Assert
        Assert.That(claim, Is.Null);
    }

    [Test]
    public async Task MarkSentAsync_WhenClaimMatches_ShouldMarkSentAndClearLockAndError()
    {
        //Arrange
        var lockId = Guid.NewGuid();

        var locked = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 1,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = lockId,
            LockedBy = Environment.MachineName,
            LockedUntil = DateTimeOffset.UtcNow.AddSeconds(30),
            LastError = "old error",
        };

        _context.Notifications.Add(locked);
        await _context.SaveChangesAsync();

        var notification = await _context.Notifications.AsNoTracking().SingleAsync(e => e.Id == locked.Id);
        var claim = new NotificationClaim(notification.Id, notification, lockId);

        //Act
        await _coordinator.MarkSentAsync(claim);

        //Assert
        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == locked.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Sent));
            Assert.That(updated.SentAt, Is.Not.Null);
            Assert.That(updated.LastError, Is.Null);
            Assert.That(updated.LockId, Is.Null);
            Assert.That(updated.LockedBy, Is.Null);
            Assert.That(updated.LockedUntil, Is.Null);
        }
    }

    [Test]
    public async Task MarkSentAsync_WhenLockIdDoesNotMatch_ShouldNotUpdateNotification()
    {
        //Arrange
        var actualLockId = Guid.NewGuid();

        var locked = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 1,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = actualLockId,
            LockedBy = Environment.MachineName,
            LockedUntil = DateTimeOffset.UtcNow.AddSeconds(30),
        };

        _context.Notifications.Add(locked);
        await _context.SaveChangesAsync();

        var notification = await _context.Notifications.AsNoTracking().SingleAsync(e => e.Id == locked.Id);
        var claim = new NotificationClaim(70, notification, Guid.NewGuid());

        //Act
        await _coordinator.MarkSentAsync(claim);

        //Assert
        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == locked.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.SentAt, Is.Null);
            Assert.That(updated.LockId, Is.EqualTo(actualLockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
        }
    }

    [Test]
    public async Task MarkFailedAsync_WhenAttemptCountIsBelowTen_ShouldMarkPendingStoreErrorAndClearLock()
    {
        //Arrange
        var lockId = Guid.NewGuid();

        var locked = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 3,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = lockId,
            LockedBy = Environment.MachineName,
            LockedUntil = DateTimeOffset.UtcNow.AddSeconds(30),
        };

        _context.Notifications.Add(locked);
        await _context.SaveChangesAsync();

        var notification = await _context.Notifications.AsNoTracking().SingleAsync(e => e.Id == locked.Id);
        var claim = new NotificationClaim(notification.Id, notification, lockId);
        var exception = new InvalidOperationException("Dispatch failed.");

        //Act
        await _coordinator.MarkFailedAsync(claim, exception);

        //Assert
        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == locked.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Pending));
            Assert.That(updated.LastError, Does.Contain("Dispatch failed."));
            Assert.That(updated.LockId, Is.Null);
            Assert.That(updated.LockedBy, Is.Null);
            Assert.That(updated.LockedUntil, Is.Null);
        }
    }

    [Test]
    public async Task MarkFailedAsync_WhenAttemptCountIsTen_ShouldMarkFailedStoreErrorAndClearLock()
    {
        //Arrange
        var lockId = Guid.NewGuid();

        var locked = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 10,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = lockId,
            LockedBy = Environment.MachineName,
            LockedUntil = DateTimeOffset.UtcNow.AddSeconds(30),
        };

        _context.Notifications.Add(locked);
        await _context.SaveChangesAsync();

        var notification = await _context.Notifications.AsNoTracking().SingleAsync(e => e.Id == locked.Id);
        var claim = new NotificationClaim(notification.Id, notification, lockId);
        var exception = new InvalidOperationException("Dispatch failed at limit.");

        //Act
        await _coordinator.MarkFailedAsync(claim, exception);

        //Assert
        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == locked.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Failed));
            Assert.That(updated.LastError, Does.Contain("Dispatch failed at limit."));
            Assert.That(updated.LockId, Is.Null);
            Assert.That(updated.LockedBy, Is.Null);
            Assert.That(updated.LockedUntil, Is.Null);
        }
    }

    [Test]
    public async Task MarkFailedAsync_WhenLockIdDoesNotMatch_ShouldNotUpdateNotification()
    {
        //Arrange
        var actualLockId = Guid.NewGuid();

        var locked = new Notification
        {
            SubscriptionId = _subscriptions[0].Id,
            Subscription = _subscriptions[0],
            EventId = _subscriptions[0].EventId,
            ChannelId = _subscriptions[0].ChannelId,
            ScheduleId = _subscriptions[0].ScheduleId,
            Status = NotificationStatusType.InFlight,
            AttemptCount = 2,
            PayloadType = _payloadType,
            Payload = _payload,
            LockId = actualLockId,
            LockedBy = Environment.MachineName,
            LockedUntil = DateTimeOffset.UtcNow.AddSeconds(30),
        };

        _context.Notifications.Add(locked);
        await _context.SaveChangesAsync();

        var notification = await _context.Notifications.AsNoTracking().SingleAsync(e => e.Id == locked.Id);
        var claim = new NotificationClaim(notification.Id, notification, Guid.NewGuid());

        //Act
        await _coordinator.MarkFailedAsync(claim, new InvalidOperationException("Should not apply."));

        //Assert
        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == locked.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.LastError, Is.Null);
            Assert.That(updated.LockId, Is.EqualTo(actualLockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
        }
    }

    private sealed class TestNotificationDbContext : DbContext
    {
        public TestNotificationDbContext(DbContextOptions<TestNotificationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Notification> Notifications => Set<Notification>();

        public DbSet<NotificationSubscription> NotificationSubscriptions => Set<NotificationSubscription>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Notification>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.Subscription)
                    .WithMany()
                    .HasForeignKey(e => e.SubscriptionId);

                entity.Property(e => e.Status);
            });

            modelBuilder.Entity<NotificationSubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
            });

            var jsonOptions = new JsonSerializerOptions();
            jsonOptions.Converters.Add(new ObjectToInferredTypesConverter());

            modelBuilder.AddGlobalValueConverter(new ValueConverter<IDictionary<string, object?>, string>(
                app => JsonSerializer.Serialize(app, jsonOptions),
                db => JsonSerializer.Deserialize<IDictionary<string, object?>>(db, jsonOptions)!),
                new ValueComparer<IDictionary<string, object?>>(
                    (a, b) => JsonSerializer.Serialize(a, jsonOptions) == JsonSerializer.Serialize(b, jsonOptions),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => new Dictionary<string, object?>(c)));

            modelBuilder.AddGlobalValueConverter(new ValueConverter<IDictionary<string, string?>, string>(
                app => JsonSerializer.Serialize(app, jsonOptions),
                db => JsonSerializer.Deserialize<IDictionary<string, string?>>(db, jsonOptions)!),
                new ValueComparer<IDictionary<string, string?>>(
                    (a, b) => JsonSerializer.Serialize(a, jsonOptions) == JsonSerializer.Serialize(b, jsonOptions),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => new Dictionary<string, string?>(c)));
        }
    }

    private class TestDomainEvent : IDomainEvent;
}
