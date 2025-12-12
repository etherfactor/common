using System.Collections.Immutable;

namespace EtherGizmos.Common.Abstractions;

public record SentMessage
{
    public required string Type { get; init; }

    public required string Body { get; init; }

    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    public required string LogicalDestinationName { get; init; }

    internal IDictionary<string, string> OpenTelemetryHeaders { get; set; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> AllHeaders => Headers
        .Concat(OpenTelemetryHeaders)
        .Append(new KeyValuePair<string, string>("$type", Type))
        .Append(new KeyValuePair<string, string>("$logical", LogicalDestinationName))
        .ToImmutableDictionary();
}
