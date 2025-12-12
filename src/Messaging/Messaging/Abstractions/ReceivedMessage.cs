namespace EtherGizmos.Common.Abstractions;

public record ReceivedMessage
{
    public required string Id { get; init; }

    public required string Type { get; init; }

    public required string Body { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public required string LogicalSourceName { get; init; }

    public required IMessageActions Actions { get; init; }

    public string? ConsumerName { get; init; }
}
