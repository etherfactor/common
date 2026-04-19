namespace EtherGizmos.Common.Abstractions;

public record BusKey(
    string BusId,
    string? LogicalName = null
);
