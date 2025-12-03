using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        var matches = AbstractTypeRegistry.Registrations
            .Where(e => e.BaseType == typeof(DatabaseConnectionOptions))
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(factory, Is.Not.Null);
            Assert.That(matches, Has.One.Matches<AbstractTypeRegistration>(reg =>
                reg.Properties.Contains(typeof(RootPostgreSqlOptions)
                    .GetProperty(nameof(RootPostgreSqlOptions.PostgreSql))!)));
        }
    }
}
