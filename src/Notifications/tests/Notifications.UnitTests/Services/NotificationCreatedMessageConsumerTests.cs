using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EtherGizmos.Common.Services;

internal class NotificationCreatedMessageConsumerTests
{
    private Lazy<NotificationCreatedMessageConsumer> _consumer;
    private Mock<IKeyedServiceProvider> _serviceProviderMock;
    private Mock<INotificationHandler> _handlerMock;
    private Mock<IMessageContext<NotificationCreatedMessage>> _contextMock;

    [SetUp]
    public void SetUp()
    {
        _serviceProviderMock = new();

        _serviceProviderMock.Setup(@interface =>
            @interface.GetRequiredKeyedService(typeof(INotificationHandler), ImmediateSchedule.Instance.Key))
            .Returns(() => _handlerMock.Object);

        _handlerMock = new();

        _contextMock = new();

        _contextMock.Setup(@interface =>
            @interface.Message)
            .Returns(new NotificationCreatedMessage()
            {
                NotificationId = 123,
                ScheduleType = ImmediateSchedule.Instance.Key,
            });

        _consumer = new(() => new(
            _serviceProviderMock.Object));
    }

    [Test]
    public async Task ConsumeAsync_WithMessage_ShouldInvokeHandlerForSchedule()
    {
        //Arrange
        var consumer = _consumer.Value;

        //Act
        await consumer.ConsumeAsync(_contextMock.Object);

        //Assert
        _serviceProviderMock.Verify(e => e.GetRequiredKeyedService(typeof(INotificationHandler), ImmediateSchedule.Instance.Key), Times.Once());
        _handlerMock.Verify(e => e.HandleAsync(123, It.IsAny<CancellationToken>()), Times.Once());
    }
}
