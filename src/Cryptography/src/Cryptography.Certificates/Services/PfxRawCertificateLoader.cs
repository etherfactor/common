using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace EtherGizmos.Common.Services;

internal class PfxRawCertificateLoader : ICertificateLoader<PfxRawCertificateOptions>
{
    public X509Certificate2 Load(
        PfxRawCertificateOptions options)
    {
        var certificateBytes = Convert.FromBase64String(options.CertificateBase64);

        var certificate = X509CertificateLoader.LoadPkcs12(certificateBytes, options.Password);
        return certificate;
    }

    public Task<X509Certificate2> LoadAsync(
        PfxRawCertificateOptions options,
        CancellationToken cancellationToken = default)
    {
        var certificate = Load(options);
        return Task.FromResult(certificate);
    }
}
