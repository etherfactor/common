using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationCollectorTests
{
    [Test]
    public void Delay_WhenRead_ShouldReturnFiveMinutes()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationCollector>>();
        var sender = new Mock<INotificationDispatcher>();
        var coordinator = new Mock<INotificationLockingCoordinator>();

        var collector = new TestableImmediateNotificationCollector(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act
        var result = collector.Delay;

        // Assert
        Assert.That(result, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public async Task CollectBatchAsync_WhenClaimBatchReturnsNoClaims_ShouldNotDispatchOrMarkAnything()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationCollector>>();
        var sender = new Mock<INotificationDispatcher>();
        var coordinator = new Mock<INotificationLockingCoordinator>();

        coordinator
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var collector = new TestableImmediateNotificationCollector(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act
        await collector.InvokeCollectBatchAsync();

        // Assert
        sender.Verify(e => e.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        coordinator.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        coordinator.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CollectBatchAsync_WhenDispatchSucceeds_ShouldMarkSent()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationCollector>>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();
        var uow = new Mock<IUnitOfWork>();
        var sender = new Mock<INotificationDispatcher>();
        var coordinator = new Mock<INotificationLockingCoordinator>();

        var notification = new Notification { Id = 123 };
        var claim = new NotificationClaim(123, notification, Guid.NewGuid());

        uowFactory
            .Setup(e => e.Create())
            .Returns(uow.Object);

        coordinator
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([claim]);

        sender
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        coordinator
            .Setup(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collector = new TestableImmediateNotificationCollector(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act
        await collector.InvokeCollectBatchAsync();

        // Assert
        sender.Verify(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        coordinator.Verify(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()), Times.Once);
        coordinator.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CollectBatchAsync_WhenDispatchThrows_ShouldMarkFailed()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationCollector>>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();
        var uow = new Mock<IUnitOfWork>();
        var sender = new Mock<INotificationDispatcher>();
        var coordinator = new Mock<INotificationLockingCoordinator>();

        var notification = new Notification { Id = 123 };
        var claim = new NotificationClaim(123, notification, Guid.NewGuid());
        var exception = new InvalidOperationException("Boom");

        uowFactory
            .Setup(e => e.Create())
            .Returns(uow.Object);

        coordinator
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([claim]);

        sender
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        coordinator
            .Setup(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collector = new TestableImmediateNotificationCollector(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act
        await collector.InvokeCollectBatchAsync();

        // Assert
        coordinator.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        coordinator.Verify(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged(logger, LogLevel.Error, "Failed to send notification");
        VerifyLogged(logger, LogLevel.Warning, "Failed to send 1 notifications");
    }

    [Test]
    public async Task CollectBatchAsync_WhenMultipleDispatchesFail_ShouldLogWarningWithFailureCount()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationCollector>>();
        var uowFactory = new Mock<IUnitOfWorkFactory>();
        var uow = new Mock<IUnitOfWork>();
        var sender = new Mock<INotificationDispatcher>();
        var coordinator = new Mock<INotificationLockingCoordinator>();

        var notification1 = new Notification { Id = 1 };
        var notification2 = new Notification { Id = 2 };
        var claim1 = new NotificationClaim(1, notification1, Guid.NewGuid());
        var claim2 = new NotificationClaim(2, notification2, Guid.NewGuid());

        uowFactory
            .Setup(e => e.Create())
            .Returns(uow.Object);

        coordinator
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([claim1, claim2]);

        sender
            .Setup(e => e.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Boom"));

        coordinator
            .Setup(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collector = new TestableImmediateNotificationCollector(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act
        await collector.InvokeCollectBatchAsync();

        // Assert
        VerifyLogged(logger, LogLevel.Warning, "Failed to send 2 notifications");
    }

    private static void VerifyLogged(
        Mock<ILogger<ImmediateNotificationCollector>> logger,
        LogLevel level,
        string containsMessage)
    {
        logger.Verify(
            e => e.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(containsMessage, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private sealed class TestableImmediateNotificationCollector : ImmediateNotificationCollector
    {
        public TestableImmediateNotificationCollector(
            ILogger<ImmediateNotificationCollector> logger,
            INotificationDispatcher sender,
            INotificationLockingCoordinator coordinator)
            : base(logger, sender, coordinator)
        {
        }

        public Task InvokeCollectBatchAsync(CancellationToken cancellationToken = default)
            => CollectBatchAsync(cancellationToken);
    }
}
