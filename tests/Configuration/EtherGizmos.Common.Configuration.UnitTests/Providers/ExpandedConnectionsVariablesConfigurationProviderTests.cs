using Microsoft.Extensions.Configuration;

namespace EtherGizmos.Common.Configuration.UnitTests.Providers;

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
            (new(@"(?<=[^:_])_(?=[^_])"), "."));

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
            (new(@"(?<=[^_]):_(?=[^_])"), " "));

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
            (new(@"(?<=[^_]):_(?=[^_])"), " "));

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
            (new(@"^ConnectionStrings:(?=[^_:])"), ""));

        //Assert
        Assert.That(_config.GetValue<string>("Hello:Url"), Is.EqualTo("https://hello"));
    }
}
