namespace EtherGizmos.Common.Services;

internal class PostgreSqlDbConnectionFactoryTests
{
    private const string ConnectionString = "host=fake_host;port=1234;database=fake_db;user id=fake_user;password=fake_password";

    [Test]
    public void CreateDbConnection_WhenCalled_ShouldReturnDbConnection()
    {
        //Arrange
        var factory = new PostgreSqlDbConnectionFactory();

        //Act
        using var connection = factory.Create(new() { ConnectionString = ConnectionString });

        //Assert
        Assert.That(connection.ConnectionString, Is.EqualTo(ConnectionString));
    }
}
