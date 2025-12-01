using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common;

internal class PostgreSqlConnectionResolverBuilderExtensionsTests
{
    [Test]
    public void WithPostgreSql_WhenCalled_ShouldAddServices()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddConnectionResolver()
            .WithPostgreSql();

        using var provider = services.BuildServiceProvider();

        //Assert
        var factory = provider.GetService<IDbConnectionFactory<PostgreSqlOptions>>();
        var options = provider.GetRequiredService<IOptionsMonitor<AbstractTypeOptions>>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(factory, Is.Not.Null);
            Assert.That(options.CurrentValue.ConnectionMap, Does.ContainKey(typeof(ConnectionOptions)));
        }

        var set = options.CurrentValue.ConnectionMap[typeof(ConnectionOptions)];

        Assert.That(set, Does.Contain(typeof(RootPostgreSqlOptions)));
    }
}
