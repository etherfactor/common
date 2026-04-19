namespace EtherGizmos.Common.Abstractions;

public sealed record DigestMode() : DeliveryMode("digest")
{
    public static DigestMode Instance { get; } = new();
}
