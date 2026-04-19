using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EtherGizmos.Common;

internal class KeyResolverExtensionsTests
{
    private const string CertFile = "cert.pfx";

    [TearDown]
    public void TearDown()
    {
        if (File.Exists(CertFile))
            File.Delete(CertFile);
    }

    [Test]
    public void GetAsymmetricKey_WhenCalled_ShouldReturnConnection()
    {
        //Arrange
        var services = new ServiceCollection();

        services.AddKeyResolver()
            .WithCertificates();

        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Keys:TestKey:Type"] = "Asymmetric",
            ["Keys:TestKey:PfxFile:Path"] = CertFile,
        });

        services.AddSingleton<IConfiguration>(config);

        var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IKeyResolver>();

        //Act
        var key = resolver.GetAsymmetricKey("TestKey");

        //Assert
        Assert.That(key, Is.Not.Null);
    }

    [Test]
    public void LoadCertificate_WhenCalled_ShouldReturnDbConnection()
    {
        //Arrange
        var services = new ServiceCollection();

        services.AddKeyResolver()
            .WithCertificates();

        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Keys:TestKey:Type"] = "Asymmetric",
            ["Keys:TestKey:PfxFile:Path"] = CertFile,
        });

        services.AddSingleton<IConfiguration>(config);

        var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IKeyResolver>();

        //Act
        var cert = resolver.LoadCertificate("TestKey");

        //Assert
        Assert.That(cert, Is.Not.Null);
    }

    [Test]
    public async Task LoadCertificateAsync_WhenCalled_ShouldReturnDbConnection()
    {
        //Arrange
        var services = new ServiceCollection();

        services.AddKeyResolver()
            .WithCertificates();

        var config = new ConfigurationManager();
        config.AddInMemoryCollection(new Dictionary<string, string?>()
        {
            ["Keys:TestKey:Type"] = "Asymmetric",
            ["Keys:TestKey:PfxFile:Path"] = CertFile,
        });

        services.AddSingleton<IConfiguration>(config);

        var provider = services.BuildServiceProvider();

        var resolver = provider.GetRequiredService<IKeyResolver>();

        //Act
        var cert = await resolver.LoadCertificateAsync("TestKey");

        //Assert
        Assert.That(cert, Is.Not.Null);
    }
}
