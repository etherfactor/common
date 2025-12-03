using EtherGizmos.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Providers;

internal class ExpandedConnectionsVariablesConfigurationProviderTests
{
    private ConfigurationManager _config;

    [SetUp]
    public void SetUp()
    {
        _config = new();
    }

    [Test]
    public void AddRemappedEnvironmentVariables_WithPeriod_ShouldRemap()
    {
        //Arrange
        Environment.SetEnvironmentVariable("With_Period", "period");

        //Act
        _config.AddRemappedEnvironmentVariables(
            new Remap(
                From: new(@"(?<=[^:_])_(?=[^_])"),
                To: ".")!);

        //Assert
        Assert.That(_config.GetValue<string>("With.Period"), Is.EqualTo("period"));
    }

    [Test]
    public void AddRemappedEnvironmentVariables_WithSpace_ShouldRemap()
    {
        //Arrange
        Environment.SetEnvironmentVariable("With___Space", "space");

        //Act
        _config.AddRemappedEnvironmentVariables(
            new Remap(
                From: new(@"(?<=[^_]):_(?=[^_])"),
                To: " ")!);

        //Assert
        Assert.That(_config.GetValue<string>("With Space"), Is.EqualTo("space"));
    }

    [Test]
    public void AddRemappedEnvironmentVariables_WhenExists_ShouldNotRemap()
    {
        //Arrange
        Environment.SetEnvironmentVariable("I___Exist", "false");
        Environment.SetEnvironmentVariable("I Exist", "true");

        //Act
        _config.AddRemappedEnvironmentVariables(
            new Remap(
                From: new(@"(?<=[^_]):_(?=[^_])"),
                To: " ")!);

        //Assert
        Assert.That(_config.GetValue<string>("I Exist"), Is.EqualTo("true"));
    }

    [Test]
    public void AddRemappedEnvironmentVariables_WithPrefix_ShouldRemap()
    {
        //Arrange
        Environment.SetEnvironmentVariable("ConnectionStrings:Hello:Url", "https://hello");

        //Act
        _config.AddRemappedEnvironmentVariables(
            new Remap(
                From: new(@"^ConnectionStrings:(?=[^_:])"),
                To: "")!);

        //Assert
        Assert.That(_config.GetValue<string>("Hello:Url"), Is.EqualTo("https://hello"));
    }

    [Test]
    public void AddExpandedConnections_WithTerm_ShouldExpand()
    {
        //Act
        new ServiceCollection()
            .AddConnectionResolver()
            .WithPostgreSql();

        var cstr = "host=fake_host;port=1234;database=fake_db;user id=fake_user;password=fake_password";
        _config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Artifacts:PostgreSql:ConnectionString"] = cstr,
        });

        _config.AddExpandedConnections(_config);

        //Assert
        var connectionId = _config.GetValue<string>("Artifacts:ConnectionId");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(connectionId, Is.Not.Null);
            Assert.That(_config.GetValue<string>($"Connections:{connectionId}:PostgreSql:ConnectionString"), Is.EqualTo(cstr));
        }
    }

    [Test]
    public void AddExpandedConnections_WithTwoTerms_ShouldExpandBoth()
    {
        //Act
        new ServiceCollection()
            .AddConnectionResolver()
            .WithPostgreSql();

        var cstr1 = "host=fake_host;port=1234;database=fake_db_artifacts;user id=fake_user;password=fake_password";
        var cstr2 = "host=fake_host;port=1234;database=fake_db_oauth2;user id=fake_user;password=fake_password";
        _config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Artifacts:PostgreSql:ConnectionString"] = cstr1,
            ["Security:OAuth2:PostgreSql:ConnectionString"] = cstr2,
        });

        _config.AddExpandedConnections(_config);

        //Assert
        var artifactsConnectionId = _config.GetValue<string>("Artifacts:ConnectionId");
        var oauth2ConnectionId = _config.GetValue<string>("Security:OAuth2:ConnectionId");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(artifactsConnectionId, Is.Not.Null);
            Assert.That(oauth2ConnectionId, Is.Not.Null);
            Assert.That(artifactsConnectionId, Is.Not.EqualTo(oauth2ConnectionId));
            Assert.That(_config.GetValue<string>($"Connections:{artifactsConnectionId}:PostgreSql:ConnectionString"), Is.EqualTo(cstr1));
            Assert.That(_config.GetValue<string>($"Connections:{oauth2ConnectionId}:PostgreSql:ConnectionString"), Is.EqualTo(cstr2));
        }
    }
}
