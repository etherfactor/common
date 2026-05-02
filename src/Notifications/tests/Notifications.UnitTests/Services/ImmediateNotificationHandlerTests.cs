using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationHandlerTests
{
    private Lazy<ImmediateNotificationHandler> _handler;
    private Mock<ILogger<ImmediateNotificationHandler>> _loggerMock;
    private Mock<INotificationDispatcher> _dispatcherMock;
    private Mock<INotificationLockingCoordinator> _coordinatorMock;

    [SetUp]
    public void SetUp()
    {
        _loggerMock = new();

        _dispatcherMock = new();

        _coordinatorMock = new();

        _handler = new(() => new(
            _loggerMock.Object,
            _dispatcherMock.Object,
            _coordinatorMock.Object));
    }

    [Test]
    public async Task HandleAsync_WhenClaimSingleReturnsNull_ShouldLogWarningAndReturn()
    {
        //Arrange
        _coordinatorMock
            .Setup(e => e.ClaimSingleAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationClaim?)null);

        var handler = _handler.Value;

        //Act
        await handler.HandleAsync(123);

        //Assert
        _dispatcherMock.Verify(e => e.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        _coordinatorMock.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        _coordinatorMock.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLogged(_loggerMock, LogLevel.Warning, "Failed to claim notification");
    }

    [Test]
    public async Task HandleAsync_WhenDispatchSucceeds_ShouldMarkSent()
    {
        //Arrange
        var notification = new Notification { Id = 123, NotificationSubscription = new() { } };
        var claim = new NotificationClaim(123, notification, Guid.NewGuid());

        _coordinatorMock
            .Setup(e => e.ClaimSingleAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        _dispatcherMock
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _coordinatorMock
            .Setup(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = _handler.Value;

        //Act
        await handler.HandleAsync(123);

        //Assert
        _dispatcherMock.Verify(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        _coordinatorMock.Verify(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()), Times.Once);
        _coordinatorMock.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_WhenDispatchThrows_ShouldMarkFailedAndRethrow()
    {
        //Arrange
        var notification = new Notification { Id = 123, NotificationSubscription = new() { } };
        var claim = new NotificationClaim(123, notification, Guid.NewGuid());
        var exception = new InvalidOperationException("Boom");

        _coordinatorMock
            .Setup(e => e.ClaimSingleAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        _dispatcherMock
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        _coordinatorMock
            .Setup(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = _handler.Value;

        //Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(123));

        _coordinatorMock.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        _coordinatorMock.Verify(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged(_loggerMock, LogLevel.Error, "Failed to send notification");
    }

    private static void VerifyLogged(
        Mock<ILogger<ImmediateNotificationHandler>> logger,
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
}
