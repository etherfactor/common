using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class CertificateKeyResolverBuilderExtensions
{
    extension(IKeyResolverBuilder @this)
    {
        public IKeyResolverBuilder WithCertificates()
        {
            @this.Services.TryAddSingleton<ICertificateLoader<PfxFileCertificateOptions>, PfxFileCertificateLoader>();

            ModularConfigurationTypeRegistry.Register<RootCertificateOptions, AsymmetricKeyOptions>(
                sectionName: "Keys",
                itemIdName: "KeyId",
                typeName: KeyType.Asymmetric);

            ModularConfigurationTypeRegistry.Register<RootCertificateOptions, SymmetricKeyOptions>(
                sectionName: "Keys",
                itemIdName: "KeyId",
                typeName: KeyType.Asymmetric);

            return @this;
        }
    }
}
