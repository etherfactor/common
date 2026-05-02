using EtherGizmos.Common.Configuration;

namespace EtherGizmos.Common;

internal class PostgreSqlDatabaseConnectionOptionsExtensionsTests
{
    [Test]
    public void IsPostgreSql_WhenPostgreSql_ShouldReturnTrue()
    {
        //Arrange
        var options = new PostgreSqlOptions();

        //Act
        var result = options.IsPostgreSql(out var postgres);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(postgres, Is.Not.Null);
        }
    }

    [Test]
    public void IsPostgreSql_WhenNotPostgreSql_ShouldReturnFalse()
    {
        //Arrange
        var options = new DatabaseConnectionOptions();

        //Act
        var result = options.IsPostgreSql(out var postgres);

        //Assert
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(postgres, Is.Null);
        }
    }
}
