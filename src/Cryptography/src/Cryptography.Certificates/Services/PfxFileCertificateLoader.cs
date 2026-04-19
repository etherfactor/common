using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EtherGizmos.Common.Services;

internal class PfxFileCertificateLoader : ICertificateLoader<PfxFileCertificateOptions>
{
    public X509Certificate2 Load(PfxFileCertificateOptions options)
    {
        if (!File.Exists(options.Path))
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest(
                "CN=auto",
                rsa,
                HashAlgorithmName.SHA256,
                RSASignaturePadding.Pkcs1);

            var now = DateTimeOffset.UtcNow;
            var signed = request.CreateSelfSigned(now, now.AddYears(100));

            var bytes = signed.Export(X509ContentType.Pfx, options.Password);
            File.WriteAllBytes(options.Path, bytes);
        }

        var certificate = X509CertificateLoader.LoadPkcs12FromFile(options.Path, options.Password);
        return certificate;
    }

    public Task<X509Certificate2> LoadAsync(
        PfxFileCertificateOptions options,
        CancellationToken cancellationToken = default)
    {
        var certificate = Load(options);
        return Task.FromResult(certificate);
    }
}
