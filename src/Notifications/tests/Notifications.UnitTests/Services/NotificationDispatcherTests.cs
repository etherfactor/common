using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace EtherGizmos.Common.Services;

internal class NotificationDispatcherTests
{
    private Lazy<NotificationDispatcher> _dispatcher;
    private IServiceProvider _serviceProvider;
    private Mock<INotificationChannelFormatter> _formatterMock;
    private Mock<INotificationChannelSender> _senderMock;
    private Mock<INotificationEnvelope> _envelopeMock;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        _formatterMock = new();

        _formatterMock.Setup(@interface =>
            @interface.Format(It.IsAny<Notification>(), It.IsAny<object>()))
            .Returns(() => _envelopeMock.Object);

        _senderMock = new();

        _envelopeMock = new();

        services.AddKeyedSingleton(("test", typeof(TestDomainEvent)), (_, _) => _formatterMock.Object);
        services.AddKeyedSingleton("test", (_, _) => _senderMock.Object);

        _serviceProvider = services.BuildServiceProvider();

        var serializer = new DomainEventSerializer();

        _dispatcher = new(() => new(
            _serviceProvider,
            serializer));
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    public async Task DispatchAsync_WhenCalled_ShouldUseChannelOnNotification()
    {
        //Arrange
        var serializer = new DomainEventSerializer();

        var model = new TestDomainEvent();
        var serialized = serializer.Serialize(model);

        var notification = new Notification()
        {
            PayloadType = serialized.Type,
            Payload = serialized.Payload,
            NotificationSubscription = new()
            {
                ChannelKey = "test",
            },
        };

        var dispatcher = _dispatcher.Value;

        //Act
        await dispatcher.DispatchAsync(notification);

        //Assert
        _formatterMock.Verify(e => e.Format(notification, It.IsAny<TestDomainEvent>()), Times.Once());
        _senderMock.Verify(e => e.SendAsync(_envelopeMock.Object, It.IsAny<CancellationToken>()), Times.Once());
    }

    private class TestDomainEvent : IDomainEvent;
}
