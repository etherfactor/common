using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MockQueryable;
using Moq;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;

namespace EtherGizmos.Common.Services;

internal class DomainEventMessageConsumerTests
{
    private Lazy<DomainEventMessageConsumer> _consumer;
    private IServiceProvider _serviceProvider;
    private Mock<ILogger<DomainEventMessageConsumer>> _loggerMock;
    private Mock<IUnitOfWorkFactory> _uowFactoryMock;
    private Mock<IUnitOfWork> _uowMock;
    private Mock<IRepository<Notification>> _notificationRepoMock;
    private Mock<IMessageSender> _messageSenderMock;
    private Mock<IMessageContext<DomainEventMessage>> _contextMock;
    private DomainEventMessage _message;
    private List<NotificationSubscription> _subscriptions;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationManager());

        services.AddMessaging((opt, conf) => { });

        services.AddSingleton<IDomainEventRouter<TestDomainEvent>, TestDomainRouter>();

        _serviceProvider = services.BuildServiceProvider();

        _loggerMock = new();

        _uowFactoryMock = new();

        _uowMock = new Mock<IUnitOfWork>();

        var notificationSubscriptionRepoMock = new Mock<IRepository<NotificationSubscription>>();

        notificationSubscriptionRepoMock.Setup(@interface =>
            @interface.Data)
            .Returns(() => _subscriptions.BuildMock());

        _uowMock.Setup(@interface =>
            @interface.Repository<NotificationSubscription>())
            .Returns(() => notificationSubscriptionRepoMock.Object);

        _notificationRepoMock = new Mock<IRepository<Notification>>();

        _uowMock.Setup(@interface =>
            @interface.Repository<Notification>())
            .Returns(() => _notificationRepoMock.Object);

        _uowFactoryMock.Setup(@interface =>
            @interface.Create())
            .Returns(() => _uowMock.Object);

        _uowFactoryMock.Setup(@interface =>
            @interface.Create(It.IsAny<UnitOfWorkCreateOptions>()))
            .Returns(() => _uowMock.Object);

        _messageSenderMock = new();

        _messageSenderMock.Setup(@interface =>
            @interface.Services)
            .Returns(_serviceProvider);

        _contextMock = new();

        _contextMock.Setup(@interface =>
            @interface.Message)
            .Returns(() => _message);

        _contextMock.Setup(@interface =>
            @interface.RawMessage)
            .Returns(() => new ReceivedMessage()
            {
                MessageId = "",
                Type = "type",
                Body = "{}",
                Headers = ImmutableDictionary<string, string>.Empty,
                LogicalSourceName = "source",
                SubscriptionName = "sub",
                Actions = null!,
            });

        _message = new()
        {
            EventId = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            EventType = "test.domain.event",
            Audiences = [new("Group", "1")],
            IsDerived = false,
            PayloadType = typeof(TestDomainEvent).AssemblyQualifiedName!,
            Payload = "{\"value\":\"hello\"}",
        };

        _subscriptions =
        [
            new()
            {
                Id = 1,
                UserId = "user-1",
                EventId = "test.domain.event",
                ChannelId = "email",
                ChannelConfig = new Dictionary<string, object?>(),
                ScheduleId = NotificationSchedules.Immediate.Id,
                ScheduleConfig = new Dictionary<string, object?>(),
                IsEnabled = true,
                LastNotificationAt = null,
                NextNotificationAt = null,
            },
        ];

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
    public async Task ConsumeAsync_WhenSubscriptionMatches_ShouldProduceNotification()
    {
        //Arrange
        var consumer = _consumer.Value;
        var context = _contextMock.Object;

        //Act
        await consumer.ConsumeAsync(context);

        //Assert
        _notificationRepoMock.Verify(@interface =>
            @interface.Add(
                It.Is<Notification>(e =>
                    e.SubscriptionId == _subscriptions.Single().Id)),
            Times.Once());

        _uowMock.Verify(@interface =>
            @interface.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once());

        _messageSenderMock.Verify(@interface =>
            @interface.SendAsync(
                It.Is<SentMessage>(e =>
                    e.Body == "{\"notificationId\":0,\"scheduleType\":\"immediate\"}"
                    && e.LogicalDestinationName == NotificationConstants.NotificationsLogicalName),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task ConsumeAsync_WhenSubscriptionDoesNotMatch_ShouldNotProduceNotification()
    {
        //Arrange
        _message.Audiences = [new("Group", "2")];

        var consumer = _consumer.Value;
        var context = _contextMock.Object;

        //Act
        await consumer.ConsumeAsync(context);

        //Assert
        _notificationRepoMock.Verify(@interface =>
            @interface.Add(It.IsAny<Notification>()),
            Times.Never());

        _uowMock.Verify(@interface =>
            @interface.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once());

        _messageSenderMock.Verify(@interface =>
            @interface.SendAsync(It.IsAny<SentMessage>(), It.IsAny<CancellationToken>()),
            Times.Never());
    }

    [Test]
    public async Task ConsumeAsync_WhenSubscriptionSpecified_ShouldProduceOneNotification()
    {
        //Arrange
        _message.Audiences = [new("$sub", "1")];

        var consumer = _consumer.Value;
        var context = _contextMock.Object;

        //Act
        await consumer.ConsumeAsync(context);

        //Assert
        _notificationRepoMock.Verify(@interface =>
            @interface.Add(
                It.Is<Notification>(e =>
                    e.SubscriptionId == _subscriptions.Single().Id)),
            Times.Once());

        _uowMock.Verify(@interface =>
            @interface.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once());

        _messageSenderMock.Verify(@interface =>
            @interface.SendAsync(
                It.Is<SentMessage>(e =>
                    e.Body == "{\"notificationId\":0,\"scheduleType\":\"immediate\"}"
                    && e.LogicalDestinationName == NotificationConstants.NotificationsLogicalName),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public void ConsumeAsync_WhenSendingException_ShouldNotThrow()
    {
        //Arrange
        _messageSenderMock.Setup(@interface =>
            @interface.SendAsync(It.IsAny<SentMessage>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception());

        var consumer = _consumer.Value;
        var context = _contextMock.Object;

        //Act & Assert
        Assert.DoesNotThrowAsync(async () =>
        {
            await consumer.ConsumeAsync(context);
        });
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
        public string? Value { get; set; }
    }

    private sealed class TestDomainRouter : IDomainEventRouter<TestDomainEvent>
    {
        public async IAsyncEnumerable<string> FilterScopeAsync(
            TestDomainEvent @event,
            IEnumerable<AudienceKey> audiences,
            IEnumerable<string> userIds,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var groups = audiences.Where(e => e.Kind.Equals("Group", StringComparison.OrdinalIgnoreCase))
                .Select(e => e.Id)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (var userId in userIds)
            {
                if (groups.Contains("1"))
                    yield return userId;
            }
        }
    }
}
