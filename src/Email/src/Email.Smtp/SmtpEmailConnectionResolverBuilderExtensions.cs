using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Services;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EtherGizmos.Common;

public static class SmtpEmailConnectionResolverBuilderExtensions
{
    extension(IConnectionResolverBuilder @this)
    {
        public IConnectionResolverBuilder WithSmtp()
        {
            @this.Services.TryAddSingleton<IEmailSenderFactory<SmtpEmailOptions>, SmtpEmailSenderFactory>();
            @this.Services.TryAddSingleton<ISmtpClientAdapterFactory, MailKitSmtpClientAdapterFactory>();

            ModularConfigurationTypeRegistry.Register<RootSmtpEmailOptions, EmailConnectionOptions>(
                sectionName: "Connections",
                itemIdName: "ConnectionId",
                typeName: ConnectionType.Email);

            return @this;
        }
    }
}
