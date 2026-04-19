namespace EtherGizmos.Common.Abstractions;

public sealed record EmailMethod() : DeliveryMethod("email")
{
    public static EmailMethod Instance { get; } = new();
}
