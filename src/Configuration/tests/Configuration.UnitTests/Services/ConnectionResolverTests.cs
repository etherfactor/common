using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Services;

internal class ConnectionResolverTests
{
    private ConnectionResolver _resolver;
    private IServiceProvider _provider;
    private ConfigurationManager _config;

    [SetUp]
    public void SetUp()
    {
        _config = new();
        _provider = new ServiceCollection().BuildServiceProvider();
        _resolver = new(_provider, _config);
    }

    [TearDown]
    public void TearDown()
    {
        if (_provider is IDisposable disposable)
            disposable.Dispose();
    }

    [Test]
    public void ServiceProvider_WhenRead_ShouldReturnProvider()
    {
        //Act
        var provider = _resolver.ServiceProvider;

        //Assert
        Assert.That(provider, Is.EqualTo(_provider));
    }

    [Test]
    public void Options_WhenRead_ShouldReturnOptions()
    {
        //Arrange
        _config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:ValidId:SomeKey"] = "SomeValue",
        });

        //Act
        var options = _resolver.Options;

        //Assert
        Assert.That(options, Does.ContainKey("ValidId"));
    }
}
