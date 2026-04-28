using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using System.Collections.Immutable;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

internal class MessageReceiverTests
{
    [Test]
    public void ReceiveAsync_WithInvalidLogicalName_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var busId = null as string;
        var registryMock = new Mock<IMessageBusRegistry>();
        registryMock.Setup(@interface =>
            @interface.TryGetBusId(It.IsAny<string>(), out busId))
            .Returns(false);

        var optionsMock = new Mock<IOptionsMonitor<MessagingOptions>>();
        optionsMock.Setup(@interface =>
            @interface.Get(It.IsAny<string>()))
            .Returns(new MessagingOptions() { });

        using var provider = new ServiceCollection().BuildServiceProvider();
        var sender = new MessageReceiver(provider, registryMock.Object, optionsMock.Object);

        //Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await sender.ReceiveAsync(new()
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = "TestMessage",
                Body = "{}",
                Headers = ImmutableDictionary<string, string>.Empty,
                LogicalSourceName = "unknown",
                SubscriptionName = "my.sub",
                Actions = null!,
            });
        });
    }

    [Test]
    public void ReceiveAsync_WithNoConsumers_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var busId = "MyBus";
        var registryMock = new Mock<IMessageBusRegistry>();
        registryMock.Setup(@interface =>
            @interface.TryGetBusId(It.IsAny<string>(), out busId))
            .Returns(true);

        var options = new MessagingOptions();
        options.TypeMappings.AddMap(typeof(TestMessage), "TestMessage");
        options.Build();

        var optionsMock = new Mock<IOptionsMonitor<MessagingOptions>>();
        optionsMock.Setup(@interface =>
            @interface.Get(It.IsAny<string>()))
            .Returns(options);

        var services = new ServiceCollection();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddOptions<JsonSerializerOptions>("MyBus");
        using var provider = services.BuildServiceProvider();
        var sender = new MessageReceiver(provider, registryMock.Object, optionsMock.Object);

        //Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await sender.ReceiveAsync(new()
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = "TestMessage",
                Body = "{}",
                Headers = ImmutableDictionary<string, string>.Empty,
                LogicalSourceName = "unknown",
                SubscriptionName = "my.sub",
                Actions = null!,
            });
        });
    }

    [Test]
    public void ReceiveAsync_WithException_ShouldThrowAggregateException()
    {
        //Arrange
        var busId = "MyBus";
        var registryMock = new Mock<IMessageBusRegistry>();
        registryMock.Setup(@interface =>
            @interface.TryGetBusId(It.IsAny<string>(), out busId))
            .Returns(true);

        var options = new MessagingOptions();
        options.TypeMappings.AddMap(typeof(TestMessage), "TestMessage");
        options.Build();

        var optionsMock = new Mock<IOptionsMonitor<MessagingOptions>>();
        optionsMock.Setup(@interface =>
            @interface.Get(It.IsAny<string>()))
            .Returns(options);

        var services = new ServiceCollection();
        services.AddSingleton<IMessageSerializer, JsonMessageSerializer>();
        services.AddScoped<IMessageConsumer<TestMessage>, TestConsumer>();
        services.AddOptions<JsonSerializerOptions>("MyBus");

        using var provider = services.BuildServiceProvider();
        var sender = new MessageReceiver(provider, registryMock.Object, optionsMock.Object);

        //Act & Assert
        Assert.ThrowsAsync<AggregateException>(async () =>
        {
            await sender.ReceiveAsync(new()
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = "TestMessage",
                Body = "{}",
                Headers = ImmutableDictionary<string, string>.Empty,
                LogicalSourceName = "unknown",
                SubscriptionName = "my.sub",
                Actions = null!,
            });
        });
    }

    public sealed class TestMessage
    {
        public string Value { get; init; } = null!;
    }

    internal sealed class TestConsumer : IMessageConsumer<TestMessage>
    {
        public Task ConsumeAsync(
            IMessageContext<TestMessage> context)
            => throw new Exception();
    }
}
