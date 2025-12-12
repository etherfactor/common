using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EtherGizmos.Common.Abstractions;

public interface IMessageSender
{
    IServiceProvider Services { get; }

    Task SendAsync(SentMessage message, CancellationToken cancellationToken = default);
}

public static class IMessageSenderExtensions
{
    public static async Task SendAsync<TMessage>(
        this IMessageSender @this,
        string logicalName,
        TMessage message,
        CancellationToken cancellationToken = default)
        where TMessage : class, new()
    {
        var options = @this.Services
            .GetRequiredService<IOptions<MessagingOptions>>()
            .Value;

        var type = options.ConvertType(typeof(TMessage));

        var serializer = @this.Services.GetKeyedService<IMessageSerializer>(logicalName)
            ?? @this.Services.GetRequiredService<IMessageSerializer>();

        var body = serializer.Serialize(message);

        await @this.SendAsync(new SentMessage()
        {
            Type = type,
            Body = body,
            Headers = new Dictionary<string, string>(),
            LogicalDestinationName = logicalName,
        }, cancellationToken: cancellationToken);
    }
}
