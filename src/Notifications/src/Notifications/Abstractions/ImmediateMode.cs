namespace EtherGizmos.Common.Abstractions;

public sealed record ImmediateMode() : DeliveryMode("immediate")
{
    public static ImmediateMode Instance { get; } = new();
}
