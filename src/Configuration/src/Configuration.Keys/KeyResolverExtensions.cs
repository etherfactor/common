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

        public X509Certificate2 LoadCertificate(
            string keyId)
        {
            var connection = @this.GetAsymmetricKey(keyId);
            var type = connection.GetType();

            var result = (X509Certificate2)typeof(KeyResolverExtensions)
                .GetMethod(nameof(InnerLoadCertificate), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod([type])
                .Invoke(null, [@this, connection])!;

            return result;
        }

        internal X509Certificate2 InnerLoadCertificate<TOptions>(
            TOptions options)
            where TOptions : AsymmetricKeyOptions, new()
        {
            var factory = @this.ServiceProvider.GetService<ICertificateLoader<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for loading a certificate for type {typeof(TOptions)}");

            return factory.Load(options);
        }

        public Task<X509Certificate2> LoadCertificateAsync(
            string keyId,
            CancellationToken cancellationToken = default)
        {
            var connection = @this.GetAsymmetricKey(keyId);
            var type = connection.GetType();

            var result = (Task<X509Certificate2>)typeof(KeyResolverExtensions)
                .GetMethod(nameof(InnerLoadCertificateAsync), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod([type])
                .Invoke(null, [@this, connection, cancellationToken])!;

            return result;
        }

        internal async Task<X509Certificate2> InnerLoadCertificateAsync<TOptions>(
            TOptions options,
            CancellationToken cancellationToken = default)
            where TOptions : AsymmetricKeyOptions, new()
        {
            var factory = @this.ServiceProvider.GetService<ICertificateLoader<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for loading a certificate for type {typeof(TOptions)}");

            return await factory.LoadAsync(options, cancellationToken);
        }
    }
}
