using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Collections.Immutable;

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
        MessageSendOptions? options = null,
        CancellationToken cancellationToken = default)
        where TMessage : class, new()
    {
        var messagingOptions = @this.Services
            .GetRequiredService<IOptions<MessagingOptions>>()
            .Value;

        var type = messagingOptions.ConvertType(typeof(TMessage));

        var serializer = @this.Services.GetKeyedService<IMessageSerializer>(logicalName)
            ?? @this.Services.GetRequiredService<IMessageSerializer>();

        var body = serializer.Serialize(message);

        await @this.SendAsync(new SentMessage()
        {
            MessageId = Guid.NewGuid().ToString("N"),
            Type = type,
            Body = body,
            Headers = options?.Headers?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty,
            LogicalDestinationName = logicalName,
        }, cancellationToken: cancellationToken);
    }
}
