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

    //[Test]
    //public void Load_WithPostgreSqlMarker_NormalizesToConnectionsSection()
    //{
    //    // Arrange
    //    var initialData = new Dictionary<string, string?>
    //    {
    //        // This is the "raw" key with the :PostgreSql: marker
    //        ["Services:Default:PostgreSql:ConnectionString"] = "Host=fake_host;Database=fake_db;"
    //    };

    //    var innerRoot = new ConfigurationBuilder()
    //        .AddInMemoryCollection(initialData!)
    //        .Build();

    //    // Register how PostgreSql options are mapped:
    //    //
    //    // - Items live under "Connections"
    //    // - They are referenced by "ConnectionId"
    //    // - Their Type is "Database"
    //    // - The root type has a property named "PostgreSql" that holds DatabaseConnectionOptions
    //    AbstractTypeRegistry.Register<RootPostgreSqlOptions, DatabaseConnectionOptions>(
    //        sectionName: "Connections",
    //        itemIdName: "ConnectionId",
    //        typeName: "Database",                    // or ConnectionType.Database if that's your type
    //        propertySelectors: [root => root.PostgreSql]);

    //    var provider = new ExpandedConnectionsVariablesConfigurationProvider(innerRoot);

    //    // Act
    //    provider.Load(); // populate provider.Data
    //    var normalizedRoot = new ConfigurationRoot(new[] { provider });

    //    // Assert

    //    // 1. We should have a generated ConnectionId at the original prefix
    //    var connectionId = normalizedRoot["Services:Default:ConnectionId"];
    //    Assert.That(connectionId, Is.Not.Null.And.Not.Empty, "ConnectionId should be generated for the prefix.");

    //    // 2. The normalized connection should live under "Connections:{id}:Type"
    //    var typePath = $"Connections:{connectionId}:Type";
    //    Assert.That(normalizedRoot[typePath], Is.EqualTo("Database"),
    //        "Normalized connection should be of type 'Database'.");

    //    // 3. The original PostgreSql subtree should be copied to "Connections:{id}:PostgreSql:..."
    //    var connStringPath = $"Connections:{connectionId}:PostgreSql:ConnectionString";
    //    Assert.That(normalizedRoot[connStringPath], Is.EqualTo("Host=fake_host;Database=fake_db;"));
    //}

    //[Test]
    //public void Load_WhenNoMatchingMarkers_DoesNotGenerateIds()
    //{
    //    // Arrange
    //    var initialData = new Dictionary<string, string?>
    //    {
    //        // No :PostgreSql: marker here
    //        ["Services:Default:OtherDb:ConnectionString"] = "Host=some_host;"
    //    };

    //    var innerRoot = new ConfigurationBuilder()
    //        .AddInMemoryCollection(initialData!)
    //        .Build();

    //    AbstractTypeRegistry.Register<RootPostgreSqlOptions, DatabaseConnectionOptions>(
    //        sectionName: "Connections",
    //        itemIdName: "ConnectionId",
    //        typeName: "Database",
    //        propertySelectors: [root => root.PostgreSql]);

    //    var provider = new ExpandedConnectionsVariablesConfigurationProvider(innerRoot);

    //    // Act
    //    provider.Load();
    //    var normalizedRoot = new ConfigurationRoot(new[] { provider });

    //    // Assert
    //    // No ConnectionId should have been generated at Services:Default:...
    //    var connectionId = normalizedRoot["Services:Default:ConnectionId"];
    //    Assert.That(connectionId, Is.Null.Or.Empty);
    //}

    //[Test]
    //public void Load_WithMultiplePrefixes_GeneratesSeparateConnectionIds()
    //{
    //    // Arrange
    //    var initialData = new Dictionary<string, string?>
    //    {
    //        ["ServiceA:Db:PostgreSql:ConnectionString"] = "Host=a_host;",
    //        ["ServiceB:Db:PostgreSql:ConnectionString"] = "Host=b_host;"
    //    };

    //    var innerRoot = new ConfigurationBuilder()
    //        .AddInMemoryCollection(initialData!)
    //        .Build();

    //    AbstractTypeRegistry.Register<RootPostgreSqlOptions, DatabaseConnectionOptions>(
    //        sectionName: "Connections",
    //        itemIdName: "ConnectionId",
    //        typeName: "Database",
    //        propertySelectors: [root => root.PostgreSql]);

    //    var provider = new ExpandedConnectionsVariablesConfigurationProvider(innerRoot);

    //    // Act
    //    provider.Load();
    //    var normalizedRoot = new ConfigurationRoot(new[] { provider });

    //    var connectionIdA = normalizedRoot["ServiceA:Db:ConnectionId"];
    //    var connectionIdB = normalizedRoot["ServiceB:Db:ConnectionId"];

    //    // Assert
    //    Assert.That(connectionIdA, Is.Not.Null.And.Not.Empty);
    //    Assert.That(connectionIdB, Is.Not.Null.And.Not.Empty);
    //    Assert.That(connectionIdA, Is.Not.EqualTo(connectionIdB), "Each prefix should get its own id.");

    //    Assert.That(
    //        normalizedRoot[$"Connections:{connectionIdA}:PostgreSql:ConnectionString"],
    //        Is.EqualTo("Host=a_host;"));

    //    Assert.That(
    //        normalizedRoot[$"Connections:{connectionIdB}:PostgreSql:ConnectionString"],
    //        Is.EqualTo("Host=b_host;"));
    //}
}
