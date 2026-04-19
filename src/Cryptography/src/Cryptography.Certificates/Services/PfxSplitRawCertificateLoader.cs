using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EtherGizmos.Common.Services;

internal class PfxSplitRawCertificateLoader : ICertificateLoader<PfxSplitRawCertificateOptions>
{
    public X509Certificate2 Load(
        PfxSplitRawCertificateOptions options)
    {
        var publicKey = Encoding.UTF8.GetString(Convert.FromBase64String(options.PublicKeyBase64));
        var privateKey = Encoding.UTF8.GetString(Convert.FromBase64String(options.PrivateKeyBase64));

        var certificate = X509Certificate2.CreateFromPem(publicKey, privateKey);
        return certificate;
    }

    public Task<X509Certificate2> LoadAsync(
        PfxSplitRawCertificateOptions options,
        CancellationToken cancellationToken = default)
    {
        var certificate = Load(options);
        return Task.FromResult(certificate);
    }
}
