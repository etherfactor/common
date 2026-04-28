using System.Collections.Immutable;

namespace EtherGizmos.Common.Abstractions;

public record SentMessage
{
    public required string MessageId { get; init; }

    public required string Type { get; init; }

    public required string Body { get; init; }

    public required ImmutableDictionary<string, string> Headers { get; init; }

    public required string LogicalDestinationName { get; init; }
}
