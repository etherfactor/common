using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class NotificationLockingCoordinatorTests
{
    private NotificationLockingCoordinator _coordinator;
    private TestNotificationDbContext _context;
    private IServiceProvider _serviceProvider;
    private List<NotificationSubscription> _subscriptions;
    private string _payload;
    private string _payloadType;
    private string _connectionString;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _connectionString = await Setup.CreateDatabase("locking_coordinator");
    }

    [SetUp]
    public async Task SetUp()
    {
        var services = new ServiceCollection();

        services
            .AddDbContext<TestNotificationDbContext>(opt =>
            {
                opt.UseNpgsql(_connectionString)
                    .EnableSensitiveDataLogging();
            });

        services
            .AddUnitOfWork(opt =>
            {
                opt.BindDbContext<TestNotificationDbContext>();
            });

        services.AddSingleton<INotificationLockingCoordinator, NotificationLockingCoordinator>();

        _serviceProvider = services.BuildServiceProvider();

        var scope = _serviceProvider.CreateScope();

        _context = scope.ServiceProvider.GetRequiredService<TestNotificationDbContext>();
        await _context.Database.EnsureCreatedAsync();

        _coordinator = (NotificationLockingCoordinator)scope.ServiceProvider.GetRequiredService<INotificationLockingCoordinator>();

        var immediate = new NotificationSubscription
        {
            UserId = "123",
            EventType = "test.domain.event",
            ScheduleType = NotificationSchedules.Immediate.Key,
            ScheduleConfigRaw = "{}",
            ChannelKey = "test",
            ChannelConfigRaw = "{}",
        };

        var digest = new NotificationSubscription
        {
            UserId = "123",
            EventType = "test.domain.event",
            ScheduleType = NotificationSchedules.Digest.Key,
            ScheduleConfigRaw = "{}",
            ChannelKey = "test",
            ChannelConfigRaw = "{}",
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
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        var notification2 = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        var notification3 = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
            Status = NotificationStatusType.Pending,
            AttemptCount = 0,
            PayloadType = _payloadType,
            Payload = _payload,
        };

        var notification4 = new Notification
        {
            NotificationSubscriptionId = _subscriptions[1].Id,
            NotificationSubscription = _subscriptions[1],
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

        _context.ChangeTracker.Clear();

        var notificationIds = new long[] { notification1.Id, notification2.Id, notification3.Id, notification4.Id };
        var notifications = await _context.Notifications
            .Where(e => notificationIds.Contains(e.Id))
            .OrderBy(e => e.Id).ToListAsync();

        Assert.Multiple(() =>
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
        });
    }

    [Test]
    public async Task ClaimBatchAsync_WhenExpiredInFlightNotificationExists_ShouldReclaimIt()
    {
        //Arrange
        var expired = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.AttemptCount, Is.EqualTo(3));
            Assert.That(updated.LockId, Is.EqualTo(claims[0].LockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
            Assert.That(updated.LastAttemptAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task ClaimBatchAsync_WhenNotificationIsInFlightAndNotExpired_ShouldNotClaimIt()
    {
        //Arrange
        var originalLockId = Guid.NewGuid();
        var inflight = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.AttemptCount, Is.EqualTo(2));
            Assert.That(updated.LockId, Is.EqualTo(originalLockId));
            Assert.That(updated.LockedBy, Is.EqualTo("busy-machine"));
        });
    }

    [Test]
    public async Task ClaimBatchAsync_WhenAttemptCountIsTen_ShouldNotClaimIt()
    {
        //Arrange
        var maxattempts = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Pending));
        Assert.That(updated.AttemptCount, Is.EqualTo(10));
    }

    [Test]
    public async Task ClaimSingleAsync_WhenNotificationCanBeLocked_ShouldReturnClaimAndUpdateNotification()
    {
        //Arrange
        var notification = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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
        Assert.That(claim!.NotificationId, Is.EqualTo(notification.Id));

        _context.ChangeTracker.Clear();
        var updated = await _context.Notifications.SingleAsync(e => e.Id == notification.Id);

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.AttemptCount, Is.EqualTo(1));
            Assert.That(updated.LockId, Is.EqualTo(claim.LockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
            Assert.That(updated.LastAttemptAt, Is.Not.Null);
        });
    }

    [Test]
    public async Task ClaimSingleAsync_WhenNotificationCannotBeLocked_ShouldReturnNull()
    {
        //Arrange
        var inflight = new Notification
        {
            Id = 50,
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Sent));
            Assert.That(updated.SentAt, Is.Not.Null);
            Assert.That(updated.LastError, Is.Null);
            Assert.That(updated.LockId, Is.Null);
            Assert.That(updated.LockedBy, Is.Null);
            Assert.That(updated.LockedUntil, Is.Null);
        });
    }

    [Test]
    public async Task MarkSentAsync_WhenLockIdDoesNotMatch_ShouldNotUpdateNotification()
    {
        //Arrange
        var actualLockId = Guid.NewGuid();

        var locked = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.SentAt, Is.Null);
            Assert.That(updated.LockId, Is.EqualTo(actualLockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
        });
    }

    [Test]
    public async Task MarkFailedAsync_WhenAttemptCountIsBelowTen_ShouldMarkPendingStoreErrorAndClearLock()
    {
        //Arrange
        var lockId = Guid.NewGuid();

        var locked = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Pending));
            Assert.That(updated.LastError, Does.Contain("Dispatch failed."));
            Assert.That(updated.LockId, Is.Null);
            Assert.That(updated.LockedBy, Is.Null);
            Assert.That(updated.LockedUntil, Is.Null);
        });
    }

    [Test]
    public async Task MarkFailedAsync_WhenAttemptCountIsTen_ShouldMarkFailedStoreErrorAndClearLock()
    {
        //Arrange
        var lockId = Guid.NewGuid();

        var locked = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.Failed));
            Assert.That(updated.LastError, Does.Contain("Dispatch failed at limit."));
            Assert.That(updated.LockId, Is.Null);
            Assert.That(updated.LockedBy, Is.Null);
            Assert.That(updated.LockedUntil, Is.Null);
        });
    }

    [Test]
    public async Task MarkFailedAsync_WhenLockIdDoesNotMatch_ShouldNotUpdateNotification()
    {
        //Arrange
        var actualLockId = Guid.NewGuid();

        var locked = new Notification
        {
            NotificationSubscriptionId = _subscriptions[0].Id,
            NotificationSubscription = _subscriptions[0],
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

        Assert.Multiple(() =>
        {
            Assert.That(updated.Status, Is.EqualTo(NotificationStatusType.InFlight));
            Assert.That(updated.LastError, Is.Null);
            Assert.That(updated.LockId, Is.EqualTo(actualLockId));
            Assert.That(updated.LockedBy, Is.EqualTo(Environment.MachineName));
            Assert.That(updated.LockedUntil, Is.Not.Null);
        });
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

                entity.HasOne(e => e.NotificationSubscription)
                    .WithMany()
                    .HasForeignKey(e => e.NotificationSubscriptionId);

                entity.Property(e => e.Status).HasConversion<int>();
            });

            modelBuilder.Entity<NotificationSubscription>(entity =>
            {
                entity.HasKey(e => e.Id);
            });
        }
    }

    private class TestDomainEvent : IDomainEvent;
}
