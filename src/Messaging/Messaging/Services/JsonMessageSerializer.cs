using EtherGizmos.Common.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

public class JsonMessageSerializer : IMessageSerializer
{
    private readonly object _serviceKey;
    protected readonly IOptionsMonitor<JsonSerializerOptions> _options;

    public JsonMessageSerializer(
        [ServiceKey] object serviceKey,
        IOptionsMonitor<JsonSerializerOptions> options)
    {
        _serviceKey = serviceKey;
        _options = options;
    }

    public TMessage Deserialize<TMessage>(
        string message)
        where TMessage : class, new()
    {
        return JsonSerializer.Deserialize<TMessage>(message, _options.Get($"{MessagingConstants.OptionsName}:{_serviceKey}"))!;
    }

    public string Serialize<TMessage>(
        TMessage message)
        where TMessage : class, new()
    {
        return JsonSerializer.Serialize(message, _options.Get($"{MessagingConstants.OptionsName}:{_serviceKey}"));
    }
}
