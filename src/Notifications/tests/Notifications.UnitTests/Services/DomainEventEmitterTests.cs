using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace EtherGizmos.Common.Services;

internal class DomainEventEmitterTests
{
    private Lazy<DomainEventEmitter> _emitter;
    private IServiceProvider _serviceProvider;
    private Mock<IOptionsMonitor<NotificationTypeOptions>> _notificationTypeOptionsMock;
    private Mock<IMessageSender> _messageSenderMock;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IConfiguration>(new ConfigurationManager());

        services.AddMessaging((opt, conf) => { });

        _serviceProvider = services.BuildServiceProvider();

        var serializer = new DomainEventSerializer();

        _notificationTypeOptionsMock = new();

        _notificationTypeOptionsMock.Setup(@interface =>
            @interface.CurrentValue)
            .Returns(new NotificationTypeOptions()
            {
                EventTypeMap =
                {
                    ["test.domain.event"] = typeof(TestDomainEvent),
                },
            });

        _messageSenderMock = new();

        _messageSenderMock.Setup(@interface =>
            @interface.Services)
            .Returns(_serviceProvider);

        _emitter = new(() => new(
            _notificationTypeOptionsMock.Object,
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
    public void EmitAsync_WhenEventIsNull_ShouldThrowArgumentNullException()
    {
        //Arrange
        var @event = null as IDomainEvent;

        var emitter = _emitter.Value;

        //Act & Assert
        Assert.ThrowsAsync<ArgumentNullException>(async () =>
        {
            await emitter.EmitAsync(@event!, [new AudienceKey("User", "user-1")]);
        });
    }

    [Test]
    public async Task EmitAsync_WhenEventIsUnregistered_ShouldThrowInvalidOperationException()
    {
        //Arrange
        _notificationTypeOptionsMock.Setup(@interface =>
            @interface.CurrentValue)
            .Returns(new NotificationTypeOptions());

        var @event = new TestDomainEvent()
        {
            Value = "hello",
        };

        var emitter = _emitter.Value;

        //Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await emitter.EmitAsync(@event, [new AudienceKey("User", "user-1")]);
        });
    }

    [Test]
    public async Task EmitAsync_WhenEventIsValid_ShouldCallMessageSender()
    {
        //Arrange
        var @event = new TestDomainEvent()
        {
            Value = "hello",
        };

        var emitter = _emitter.Value;

        //Act
        await emitter.EmitAsync(@event, [new AudienceKey("User", "user-1")]);

        //Assert
        _messageSenderMock.Verify(@interface =>
            @interface.SendAsync(
                It.Is<SentMessage>(e =>
                    e.Body.StartsWith("{\"eventId\"")
                    && e.Body.Contains("\"eventType\":\"test.domain.event\"")
                    && e.LogicalDestinationName == NotificationConstants.DomainEventsLogicalName
                ),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    [Test]
    public async Task EmitAsync_WhenEventIsValidDigest_ShouldCallMessageSender()
    {
        //Arrange
        var @event = new TestDomainEvent()
        {
            Value = "hello",
        };

        var digest = new Digest<TestDomainEvent>()
        {
            StartAt = DateTimeOffset.UtcNow.AddMinutes(-10),
            EndAt = DateTimeOffset.UtcNow,
            Notifications = [@event],
        };

        var emitter = _emitter.Value;

        //Act
        await emitter.EmitAsync(digest, [new AudienceKey("User", "user-1")]);

        //Assert
        _messageSenderMock.Verify(@interface =>
            @interface.SendAsync(
                It.Is<SentMessage>(e =>
                    e.Body.StartsWith("{\"eventId\"")
                    && e.Body.Contains("\"eventType\":\"test.domain.event\"")
                    && e.LogicalDestinationName == NotificationConstants.DomainEventsLogicalName
                ),
                It.IsAny<CancellationToken>()),
            Times.Once());
    }

    private sealed class TestDomainEvent : IDomainEvent
    {
        public string? Value { get; set; }
    }
}
