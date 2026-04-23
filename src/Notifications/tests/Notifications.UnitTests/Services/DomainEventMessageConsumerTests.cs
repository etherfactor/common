using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;

namespace EtherGizmos.Common.Services;

internal class DomainEventMessageConsumerTests
{
    private Lazy<DomainEventMessageConsumer> _consumer;
    private IServiceProvider _serviceProvider;
    private Mock<ILogger<DomainEventMessageConsumer>> _loggerMock;
    private Mock<IUnitOfWorkFactory> _uowFactoryMock;
    private Mock<IMessageSender> _messageSenderMock;
    private Mock<IMessageContext<DomainEventMessage>> _contextMock;
    private DomainEventMessage _message;
    private List<NotificationSubscription> _subscriptions;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        _serviceProvider = services.BuildServiceProvider();

        _loggerMock = new();

        _uowFactoryMock = new();

        var uowMock = new Mock<IUnitOfWork>();

        var notificationSubscriptionRepoMock = new Mock<IRepository<NotificationSubscription>>();

        notificationSubscriptionRepoMock.Setup(@interface =>
            @interface.Data)
            .Returns(() => _subscriptions.BuildMock());

        uowMock.Setup(@interface =>
            @interface.Repository<NotificationSubscription>())
            .Returns(() => notificationSubscriptionRepoMock.Object);

        var notificationRepoMock = new Mock<IRepository<Notification>>();

        uowMock.Setup(@interface =>
            @interface.Repository<Notification>())
            .Returns(() => notificationRepoMock.Object);

        _uowFactoryMock.Setup(@interface =>
            @interface.Create())
            .Returns(() => uowMock.Object);

        _uowFactoryMock.Setup(@interface =>
            @interface.Create(It.IsAny<UnitOfWorkCreateOptions>()))
            .Returns(() => uowMock.Object);

        _messageSenderMock = new();

        _contextMock = new();

        _contextMock.Setup(@interface =>
            @interface.Message)
            .Returns(() => _message);

        _message = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            EventType = "test.domain.message",
            Audiences = [new("Group", "1")],
            IsDerived = false,
            PayloadType = typeof(TestDomainEvent).AssemblyQualifiedName!,
            Payload = "{\"value\":\"hello\"}",
        };

        var serializer = new DomainEventSerializer();

        _consumer = new(() => new(
            _loggerMock.Object,
            _serviceProvider,
            _uowFactoryMock.Object,
            serializer,
            _messageSenderMock.Object));
    }

    [TearDown]
    public void TearDown()
    {
        if (_serviceProvider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    public async Task ConsumeAsync_WhenCalled_ShouldDoAThing()
    {
        //Arrange
        var consumer = _consumer.Value;
        var context = _contextMock.Object;

        //Act
        await consumer.ConsumeAsync(context);

        //Assert
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
        public string? Value { get; set; }
    }
}
