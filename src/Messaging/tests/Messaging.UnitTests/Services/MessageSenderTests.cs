using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Collections.Immutable;

namespace EtherGizmos.Common.Services;

internal class MessageSenderTests
{
    [Test]
    public void SendAsync_WithInvalidLogicalName_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var busId = null as string;
        var registryMock = new Mock<IMessageBusRegistry>();
        registryMock.Setup(@interface =>
            @interface.TryGetBusId(It.IsAny<string>(), out busId))
            .Returns(false);

        using var provider = new ServiceCollection().BuildServiceProvider();
        var sender = new MessageSender(provider, registryMock.Object);

        //Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await sender.SendAsync(new()
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = "TestMessage",
                Body = "{}",
                Headers = ImmutableDictionary<string, string>.Empty,
                LogicalDestinationName = "unknown",
            });
        });
    }

    [Test]
    public void SendAsync_WithInvalidBudId_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var busId = "MyBus";
        var registryMock = new Mock<IMessageBusRegistry>();
        registryMock.Setup(@interface =>
            @interface.TryGetBusId(It.IsAny<string>(), out busId))
            .Returns(true);

        var bus = null as IMessageBus;
        registryMock.Setup(@interface =>
            @interface.TryGetBus(It.IsAny<string>(), out bus))
            .Returns(false);

        using var provider = new ServiceCollection().BuildServiceProvider();
        var sender = new MessageSender(provider, registryMock.Object);

        //Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await sender.SendAsync(new()
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = "TestMessage",
                Body = "{}",
                Headers = ImmutableDictionary<string, string>.Empty,
                LogicalDestinationName = "unknown",
            });
        });
    }

    [Test]
    public void SendAsync_WithInvalidPublisherId_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var busId = "MyBus";
        var registryMock = new Mock<IMessageBusRegistry>();
        registryMock.Setup(@interface =>
            @interface.TryGetBusId(It.IsAny<string>(), out busId))
            .Returns(true);

        var publisher = null as IMessagePublisher;
        var busMock = new Mock<IMessageBus>();
        busMock.Setup(@interface =>
            @interface.TryGetPublisher(It.IsAny<string>(), out publisher))
            .Returns(false);

        var bus = busMock.Object;
        registryMock.Setup(@interface =>
            @interface.TryGetBus(It.IsAny<string>(), out bus))
            .Returns(true);

        using var provider = new ServiceCollection().BuildServiceProvider();
        var sender = new MessageSender(provider, registryMock.Object);

        //Act & Assert
        Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await sender.SendAsync(new()
            {
                MessageId = Guid.NewGuid().ToString("N"),
                Type = "TestMessage",
                Body = "{}",
                Headers = ImmutableDictionary<string, string>.Empty,
                LogicalDestinationName = "unknown",
            });
        });
    }
}
