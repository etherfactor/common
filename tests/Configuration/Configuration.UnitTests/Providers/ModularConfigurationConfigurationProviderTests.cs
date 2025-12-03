using EtherGizmos.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common.Providers;

internal class ModularConfigurationConfigurationProviderTests
{
    private ConfigurationManager _config;

    [SetUp]
    public void SetUp()
    {
        _config = new();
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

        _config.AddModularConfigurations(_config);

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

        _config.AddModularConfigurations(_config);

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
