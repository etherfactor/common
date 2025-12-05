using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class CertificateKeyResolverBuilderExtensionsTests
{
    [Test]
    public void WithCertificates_WhenCalled_ShouldAddServices()
    {
        //Arrange
        var services = new ServiceCollection();

        //Act
        services.AddKeyResolver()
            .WithCertificates();

        using var provider = services.BuildServiceProvider();

        //Assert
        var factory = provider.GetService<ICertificateLoader<PfxFileCertificateOptions>>();
        var matches = ModularConfigurationTypeRegistry.Registrations
            .Where(e => e.BaseType == typeof(AsymmetricKeyOptions))
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(factory, Is.Not.Null);
            Assert.That(matches, Has.One.Matches<ModularConfigurationTypeRegistration>(reg =>
                reg.Properties.Contains(typeof(RootCertificateOptions)
                    .GetProperty(nameof(RootCertificateOptions.PfxFile))!)));
        }
    }
}
