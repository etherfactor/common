using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace EtherGizmos.Common;

public static class KeyResolverExtensions
{
    extension(IKeyResolver @this)
    {
        public AsymmetricKeyOptions GetAsymmetricKey(
            string keyId)
        {
            var connection = @this.GetOptions<KeyOptions, AsymmetricKeyOptions>(keyId, KeyType.Asymmetric);
            return connection;
        }

        public SymmetricKeyOptions GetSymmetricKey(
            string keyId)
        {
            var connection = @this.GetOptions<KeyOptions, SymmetricKeyOptions>(keyId, KeyType.Symmetric);
            return connection;
        }

        public async Task<X509Certificate2> LoadCertificateAsync(
            string keyId,
            CancellationToken cancellationToken = default)
        {
            var connection = @this.GetAsymmetricKey(keyId);
            var type = connection.GetType();

            var result = (X509Certificate2)typeof(KeyResolverExtensions)
                .GetMethod(nameof(InnerLoadCertificateAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod([type])
                .Invoke(null, [@this, keyId, connection])!;

            return result;
        }

        internal async Task<X509Certificate2> InnerLoadCertificateAsync<TOptions>(
            string connectionId,
            TOptions options)
            where TOptions : AsymmetricKeyOptions, new()
        {
            var factory = @this.ServiceProvider.GetService<ICertificateLoader<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for loading a certificate for type {typeof(TOptions)}");

            return await factory.LoadAsync(options);
        }
    }
}
