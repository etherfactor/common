using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class SmtpEmailConnectionResolverBuilderExtensionsTests
{
    [Test]
    public void WithSmtp_WhenCalled_ShouldAddServices()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddConnectionResolver()
            .WithSmtp();

        using var provider = services.BuildServiceProvider();

        //Assert
        var factory = provider.GetService<IEmailSenderFactory<SmtpEmailOptions>>();
        var matches = ModularConfigurationTypeRegistry.Registrations
            .Where(e => e.BaseType == typeof(EmailConnectionOptions))
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(factory, Is.Not.Null);
            Assert.That(matches, Has.One.Matches<ModularConfigurationTypeRegistration>(reg =>
                reg.Properties.Contains(typeof(RootSmtpEmailOptions)
                    .GetProperty(nameof(RootSmtpEmailOptions.Smtp))!)));
        }
    }
}
