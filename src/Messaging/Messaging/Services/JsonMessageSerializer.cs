using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

public class JsonMessageSerializer : IMessageSerializer
{
    protected readonly IOptionsMonitor<JsonSerializerOptions> _options;

    public JsonMessageSerializer(
        IOptionsMonitor<JsonSerializerOptions> options)
    {
        _options = options;
    }

    public TMessage Deserialize<TMessage>(
        string message)
        where TMessage : class, new()
    {
        return JsonSerializer.Deserialize<TMessage>(message, _options.Get("Messaging"))!;
    }

    public string Serialize<TMessage>(
        TMessage message)
        where TMessage : class, new()
    {
        return JsonSerializer.Serialize(message, _options.Get("Messaging"));
    }
}
