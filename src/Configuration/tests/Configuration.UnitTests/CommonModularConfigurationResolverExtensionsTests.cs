using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel.DataAnnotations;

namespace EtherGizmos.Common;

internal class CommonModularConfigurationResolverExtensionsTests
{
    [Test]
    public void GetOptions_WhenNotExists_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var config = new ConfigurationManager();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddConnectionResolver()
            .WithPostgreSql();

        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            resolver.GetOptions<ConnectionOptions, DatabaseConnectionOptions>(
                "InvalidId",
                ConnectionType.Database);
        });
    }

    [Test]
    public void GetOptions_WhenUnexpectedType_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:ValidId:Type"] = "Email",
        });

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddConnectionResolver()
            .WithPostgreSql();

        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            resolver.GetOptions<ConnectionOptions, DatabaseConnectionOptions>(
                "ValidId",
                ConnectionType.Database);
        });
    }

    [Test]
    public void GetOptions_WhenNoMatches_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:ValidId:Type"] = "Database",
        });

        ModularConfigurationTypeRegistry.Register<RootFakeOptions, DatabaseConnectionOptions>("Connections", "ConnectionId", ConnectionType.Database);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddConnectionResolver()
            .WithPostgreSql();

        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            resolver.GetOptions<ConnectionOptions, DatabaseConnectionOptions>(
                "ValidId",
                ConnectionType.Database);
        });
    }

    [Test]
    public void GetOptions_WhenTwoOrMoreMatches_ShouldThrowInvalidOperationException()
    {
        //Arrange
        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:ValidId:Type"] = "Database",
            ["Connections:ValidId:Fake1:ConnectionString"] = "hello",
            ["Connections:ValidId:Fake2:ConnectionString"] = "hello",
        });

        ModularConfigurationTypeRegistry.Register<RootFakeOptions, DatabaseConnectionOptions>("Connections", "ConnectionId", ConnectionType.Database);
        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddConnectionResolver()
            .WithPostgreSql();

        using var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act & Assert
        Assert.Throws<InvalidOperationException>(() =>
        {
            resolver.GetOptions<ConnectionOptions, DatabaseConnectionOptions>(
                "ValidId",
                ConnectionType.Database);
        });
    }

    private class RootFakeOptions : ConnectionOptions
    {
        public Fake1Options? Fake1 { get; set; }

        public Fake2Options? Fake2 { get; set; }
    }

    private class Fake1Options : DatabaseConnectionOptions
    {
        [Required]
        public string ConnectionString { get; set; } = null!;
    }

    private class Fake2Options : DatabaseConnectionOptions
    {
        [Required]
        public string ConnectionString { get; set; } = null!;
    }
}
