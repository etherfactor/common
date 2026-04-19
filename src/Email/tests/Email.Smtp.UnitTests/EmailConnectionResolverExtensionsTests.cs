using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class EmailConnectionResolverExtensionsTests
{
    [Test]
    public void GetEmailConnection_WhenCalled_ShouldReturnConnection()
    {
        //Arrange
        var services = new ServiceCollection();

        services.AddConnectionResolver()
            .WithSmtp();

        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:TestEmail:Type"] = "Email",
            ["Connections:TestEmail:Smtp:Host"] = "localhost",
            ["Connections:TestEmail:Smtp:Username"] = "username",
            ["Connections:TestEmail:Smtp:Password"] = "password",
        });

        services.AddSingleton<IConfiguration>(config);

        var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act
        var connection = resolver.GetEmailConnection("TestEmail");

        //Assert
        if (!connection.IsSmtp(out var smtp))
            Assert.Fail("The returned type was not SmtpEmailOptions");

        Assert.That(connection, Is.InstanceOf<SmtpEmailOptions>());
    }

    [Test]
    public void CreateEmailSender_WhenCalled_ShouldReturnDbConnection()
    {
        //Arrange
        var services = new ServiceCollection();

        services.AddConnectionResolver()
            .WithSmtp();

        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Connections:TestEmail:Type"] = "Email",
            ["Connections:TestEmail:Smtp:Host"] = "localhost",
            ["Connections:TestEmail:Smtp:Username"] = "username",
            ["Connections:TestEmail:Smtp:Password"] = "password",
        });

        services.AddSingleton<IConfiguration>(config);

        var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IConnectionResolver>();

        //Act
        var connection = resolver.CreateEmailSender("TestEmail");

        //Assert
        Assert.That(connection, Is.Not.Null);
    }
}
