using EtherGizmos.Common.Abstractions;
using System.Text.Json;

namespace EtherGizmos.Common.Models;

public class DomainEventMessage
{
    public string Type { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public DomainEventMessage() { }

    public DomainEventMessage(
        IDomainEvent @event)
    {
        Type = @event.GetType().FullName!;
        Payload = JsonSerializer.Serialize(@event, JsonSerializerOptions.Default);
    }
}
