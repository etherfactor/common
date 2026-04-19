using EtherGizmos.Common.Abstractions;
using System.Text.Json;

namespace EtherGizmos.Common.Services;

internal class DomainEventSerializer : IDomainEventSerializer
{
    public (string Type, string Payload) Serialize(
        object data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var serialized = JsonSerializer.Serialize(data, JsonSerializerOptions.Web);
        var type = data.GetType().AssemblyQualifiedName!;

        return (type, serialized);
    }

    public object Deserialize(
        string type,
        string payload)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);

        var actualType = Type.GetType(type)
            ?? throw new InvalidOperationException("Unrecognized type; use the AssemblyQualifiedName");
        var data = JsonSerializer.Deserialize(payload, actualType, JsonSerializerOptions.Web)!;

        return data;
    }
}
