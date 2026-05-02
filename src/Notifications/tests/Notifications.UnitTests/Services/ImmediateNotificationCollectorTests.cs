using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationCollectorTests
{
    private Lazy<TestableImmediateNotificationCollector> _collector;
    private Mock<ILogger<ImmediateNotificationCollector>> _loggerMock;
    private Mock<INotificationDispatcher> _dispatcherMock;
    private Mock<INotificationLockingCoordinator> _coordinatorMock;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new();

        _dispatcherMock = new();

        _coordinatorMock = new();

        _collector = new(() => new(
            _loggerMock.Object,
            _dispatcherMock.Object,
            _coordinatorMock.Object));
    }

    [Test]
    public void Delay_WhenRead_ShouldReturnFiveMinutes()
    {
        //Arrange
        var collector = _collector.Value;

        //Act
        var result = collector.Delay;

        //Assert
        Assert.That(result, Is.EqualTo(TimeSpan.FromMinutes(5)));
    }

    [Test]
    public async Task CollectBatchAsync_WhenClaimBatchReturnsNoClaims_ShouldNotDispatchOrMarkAnything()
    {
        // Arrange
        _coordinatorMock
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();

        //Assert
        _dispatcherMock.Verify(e => e.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        _coordinatorMock.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        _coordinatorMock.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CollectBatchAsync_WhenDispatchSucceeds_ShouldMarkSent()
    {
        //Arrange
        var notification = new Notification { Id = 123, NotificationSubscription = new() { } };
        var claim = new NotificationClaim(123, notification, Guid.NewGuid());

        _coordinatorMock
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([claim]);

        _dispatcherMock
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _coordinatorMock
            .Setup(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();

        //Assert
        _dispatcherMock.Verify(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        _coordinatorMock.Verify(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()), Times.Once);
        _coordinatorMock.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task CollectBatchAsync_WhenDispatchThrows_ShouldMarkFailed()
    {
        //Arrange
        var notification = new Notification { Id = 123, NotificationSubscription = new() { } };
        var claim = new NotificationClaim(123, notification, Guid.NewGuid());
        var exception = new InvalidOperationException("Boom");

        _coordinatorMock
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([claim]);

        _dispatcherMock
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        _coordinatorMock
            .Setup(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();

        //Assert
        _coordinatorMock.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        _coordinatorMock.Verify(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged(_loggerMock, LogLevel.Error, "Failed to send notification");
        VerifyLogged(_loggerMock, LogLevel.Warning, "Failed to send 1 notifications");
    }

    [Test]
    public async Task CollectBatchAsync_WhenMultipleDispatchesFail_ShouldLogWarningWithFailureCount()
    {
        //Arrange
        var notification1 = new Notification { Id = 1, NotificationSubscription = new() { } };
        var notification2 = new Notification { Id = 2, NotificationSubscription = new() { } };
        var claim1 = new NotificationClaim(1, notification1, Guid.NewGuid());
        var claim2 = new NotificationClaim(2, notification2, Guid.NewGuid());

        _coordinatorMock
            .Setup(e => e.ClaimBatchAsync(
                ImmediateSchedule.Instance,
                100,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync([claim1, claim2]);

        _dispatcherMock
            .Setup(e => e.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Boom"));

        _coordinatorMock
            .Setup(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var collector = _collector.Value;

        //Act
        await collector.InvokeCollectBatchAsync();

        //Assert
        VerifyLogged(_loggerMock, LogLevel.Warning, "Failed to send 2 notifications");
    }

    private static void VerifyLogged(
        Mock<ILogger<ImmediateNotificationCollector>> logger,
        LogLevel level,
        string containsMessage)
    {
        logger.Verify(@interface =>
            @interface.Log(
                level,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains(containsMessage, StringComparison.Ordinal)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce());
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
