namespace EtherGizmos.Common.Abstractions;

public sealed record EmailAddress(string Address, string? Name = null)
{
    public static EmailAddress Empty { get; } = new(string.Empty);
}
