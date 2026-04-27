namespace EtherGizmos.Common.Exceptions;

/// <summary>
/// Thrown when a circular reference is found while resolving services from child containers during runtime.
/// </summary>
public class CircularDependencyException : Exception
{
    private readonly IEnumerable<Type> _dependencyChain;

    /// <summary>
    /// The failing dependency chain.
    /// </summary>
    public IEnumerable<Type> DependencyChain => _dependencyChain;

    internal CircularDependencyException(
        IEnumerable<Type> dependencyChain)
        : base(GenerateMessage(dependencyChain))
    {
        _dependencyChain = dependencyChain;
    }

    private static string GenerateMessage(IEnumerable<Type> dependencyChain)
    {
        return "Encountered a circular dependency. Service chain resolved as follows:" + Environment.NewLine +
            string.Join(" -> ", dependencyChain);
    }
}
