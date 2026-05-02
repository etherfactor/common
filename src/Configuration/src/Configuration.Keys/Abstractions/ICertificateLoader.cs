using EtherGizmos.Common.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace EtherGizmos.Common.Abstractions;

public interface ICertificateLoader<TOptions>
    where TOptions : AsymmetricKeyOptions, new()
{
    X509Certificate2 Load(
        TOptions options);

    Task<X509Certificate2> LoadAsync(
        TOptions options,
        CancellationToken cancellationToken = default);
}
