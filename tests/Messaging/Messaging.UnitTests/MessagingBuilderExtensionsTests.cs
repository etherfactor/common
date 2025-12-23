using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class MessagingBuilderExtensionsTests
{
    [Test]
    public void AddSerializer_WhenCalled_ShouldAddSerializer()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddMessaging((opt, provider) => { })
            .UseSerializer<TestSerializer>();

        //Assert
        Assert.That(services, Has.One.Matches<ServiceDescriptor>(desc => desc.ServiceType == typeof(IMessageSerializer) && desc.ServiceKey as BusKey == new BusKey(string.Empty)));
    }

    [Test]
    public void AddSerializer_WhenCalledWithBusId_ShouldAddSerializer()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddMessaging("MyBus", (opt, provider) => { })
            .UseSerializer<TestSerializer>();

        //Assert
        Assert.That(services, Has.One.Matches<ServiceDescriptor>(desc =>
            desc.ServiceType == typeof(IMessageSerializer) &&
            desc.ServiceKey as BusKey == new BusKey("MyBus")));
    }

    [Test]
    public void AddTransformer_WhenCalled_ShouldAddTransformer()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddMessaging((opt, provider) => { })
            .AddTransformer<TestTransformer>();

        //Assert
        Assert.That(services, Has.One.Matches<ServiceDescriptor>(desc =>
            desc.ServiceType == typeof(IMessageTransformer) &&
            desc.ServiceKey as BusKey == new BusKey(string.Empty)));
    }

    [Test]
    public void AddTransformer_WhenCalledWithBusId_ShouldAddTransformer()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddMessaging("MyBus", (opt, provider) => { })
            .AddTransformer<TestTransformer>();

        //Assert
        Assert.That(services, Has.One.Matches<ServiceDescriptor>(desc =>
            desc.ServiceType == typeof(IMessageTransformer) &&
            desc.ServiceKey as BusKey == new BusKey("MyBus")));
    }

    [Test]
    public void AddMiddleware_WhenCalled_ShouldAddMiddleware()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddMessaging((opt, provider) => { })
            .AddMiddleware<TestMiddleware>();

        //Assert
        Assert.That(services, Has.One.Matches<ServiceDescriptor>(desc =>
            desc.ServiceType == typeof(IMessageMiddleware) &&
            desc.ServiceKey as BusKey == new BusKey(string.Empty)));
    }

    [Test]
    public void AddMiddleware_WhenCalledWithBusId_ShouldAddMiddleware()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddMessaging("MyBus", (opt, provider) => { })
            .AddMiddleware<TestMiddleware>();

        //Assert
        Assert.That(services, Has.One.Matches<ServiceDescriptor>(desc =>
            desc.ServiceType == typeof(IMessageMiddleware) &&
            desc.ServiceKey as BusKey == new BusKey("MyBus")));
    }

    private class TestSerializer : IMessageSerializer
    {
        public TMessage Deserialize<TMessage>(string message) where TMessage : class, new()
        {
            throw new NotImplementedException();
        }

        public string Serialize<TMessage>(TMessage message) where TMessage : class, new()
        {
            throw new NotImplementedException();
        }
    }

    private class TestTransformer : IMessageTransformer
    {
        public Task<ReceivedMessage> UnwrapAsync(ReceivedMessage envelope, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<SentMessage> WrapAsync(SentMessage envelope, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private class TestMiddleware : IMessageMiddleware
    {
        public Task InvokeAsync(ReceivedMessage message, Func<Task> next)
        {
            throw new NotImplementedException();
        }
    }
}
