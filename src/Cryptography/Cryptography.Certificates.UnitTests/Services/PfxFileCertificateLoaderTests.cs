using EtherGizmos.Common.Services;

namespace Cryptography.Certificates.UnitTests.Services;

internal class PfxFileCertificateLoaderTests
{
    [Test]
    public async Task LoadCertificateAsync_WhenCalled_ShouldReturnCertificate()
    {
        //Arrange
        var factory = new PfxFileCertificateLoader();

        //Act
        var certificate = await factory.LoadAsync(new()
        {
            Path = "test.pfx",
            AutoGenerate = true,
        });

        //Assert
        Assert.That(certificate, Is.Not.Null);
    }
}
