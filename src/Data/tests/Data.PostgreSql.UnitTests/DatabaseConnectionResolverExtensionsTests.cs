using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class DatabaseConnectionResolverExtensionsTests
{
    [Test]
    public void GetDatabaseConnection_WhenCalled_ShouldReturnConnection()
    {
        //Arrange
        var services = new ServiceCollection();

        services.AddConnectionResolver()
            .WithPostgreSql();

        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:TestDb:Type"] = "Database",
            ["Connections:TestDb:PostgreSql:ConnectionString"] = "host=fake_host;port=1234;database=fake_db;user id=fake_user;password=fake_password",
        });

        services.AddSingleton<IConfiguration>(config);

        var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act
        var connection = resolver.GetDatabaseConnection("TestDb");

        //Assert
        if (!connection.IsPostgreSql(out var postgres))
            Assert.Fail("The returned type was not PostgreSqlOptions");

        Assert.That(connection, Is.InstanceOf<PostgreSqlOptions>());
    }

    [Test]
    public void CreateDbConnection_WhenCalled_ShouldReturnDbConnection()
    {
        //Arrange
        var services = new ServiceCollection();

        services.AddConnectionResolver()
            .WithPostgreSql();

        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:TestDb:Type"] = "Database",
            ["Connections:TestDb:PostgreSql:ConnectionString"] = "host=fake_host;port=1234;database=fake_db;user id=fake_user;password=fake_password",
        });

        services.AddSingleton<IConfiguration>(config);

        var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act
        var connection = resolver.CreateDbConnection("TestDb");

        //Assert
        Assert.That(connection, Is.Not.Null);
    }
}
