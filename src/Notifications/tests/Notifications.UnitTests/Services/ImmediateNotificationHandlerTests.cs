using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Logging;
using Moq;

namespace EtherGizmos.Common.Services;

internal class ImmediateNotificationHandlerTests
{
    [Test]
    public async Task HandleAsync_WhenClaimSingleReturnsNull_ShouldLogWarningAndReturn()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationHandler>>();
        var sender = new Mock<INotificationDispatcher>();
        var coordinator = new Mock<INotificationLockingCoordinator>();

        coordinator
            .Setup(e => e.ClaimSingleAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NotificationClaim?)null);

        var handler = new ImmediateNotificationHandler(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act
        await handler.HandleAsync(123);

        // Assert
        sender.Verify(e => e.DispatchAsync(It.IsAny<Notification>(), It.IsAny<CancellationToken>()), Times.Never);
        coordinator.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        coordinator.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyLogged(logger, LogLevel.Warning, "Failed to claim notification");
    }

    [Test]
    public async Task HandleAsync_WhenDispatchSucceeds_ShouldMarkSent()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationHandler>>();
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
            .Setup(e => e.ClaimSingleAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        sender
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        coordinator
            .Setup(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ImmediateNotificationHandler(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act
        await handler.HandleAsync(123);

        // Assert
        sender.Verify(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
        coordinator.Verify(e => e.MarkSentAsync(claim, It.IsAny<CancellationToken>()), Times.Once);
        coordinator.Verify(e => e.MarkFailedAsync(It.IsAny<NotificationClaim>(), It.IsAny<Exception>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public void HandleAsync_WhenDispatchThrows_ShouldMarkFailedAndRethrow()
    {
        // Arrange
        var logger = new Mock<ILogger<ImmediateNotificationHandler>>();
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
            .Setup(e => e.ClaimSingleAsync(123, It.IsAny<CancellationToken>()))
            .ReturnsAsync(claim);

        sender
            .Setup(e => e.DispatchAsync(notification, It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        coordinator
            .Setup(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var handler = new ImmediateNotificationHandler(
            logger.Object,
            sender.Object,
            coordinator.Object);

        // Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () => await handler.HandleAsync(123));

        coordinator.Verify(e => e.MarkSentAsync(It.IsAny<NotificationClaim>(), It.IsAny<CancellationToken>()), Times.Never);
        coordinator.Verify(e => e.MarkFailedAsync(claim, exception, It.IsAny<CancellationToken>()), Times.Once);
        VerifyLogged(logger, LogLevel.Error, "Failed to send notification");
    }

    private static void VerifyLogged(
        Mock<ILogger<ImmediateNotificationHandler>> logger,
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
}
