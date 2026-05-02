using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace EtherGizmos.Common;

public static class EmailConnectionResolverExtensions
{
    extension(IConnectionResolver @this)
    {
        public EmailConnectionOptions GetEmailConnection(
            string connectionId)
        {
            var connection = @this.GetOptions<ConnectionOptions, EmailConnectionOptions>(connectionId, ConnectionType.Email);
            return connection;
        }

        public IEmailSender CreateEmailSender(
            string connectionId)
        {
            var connection = @this.GetEmailConnection(connectionId);
            var type = connection.GetType();

            var result = (IEmailSender)typeof(EmailConnectionResolverExtensions)
                .GetMethod(nameof(InnerCreateEmailSender), BindingFlags.NonPublic | BindingFlags.Static)!
                .MakeGenericMethod([type])
                .Invoke(null, [@this, connection])!;

            return result;
        }

        internal IEmailSender InnerCreateEmailSender<TOptions>(
            TOptions options)
            where TOptions : EmailConnectionOptions, new()
        {
            var factory = @this.ServiceProvider.GetService<IEmailSenderFactory<TOptions>>()
                ?? throw new InvalidOperationException($"No factory exists for creating an email sender for type {typeof(TOptions)}");

            return factory.Create(options);
        }
    }
}
