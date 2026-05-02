namespace EtherGizmos.Common.Services;

/// <summary>
/// Contains a reference to the root parent service provider.
/// </summary>
internal class ParentServiceProviderSingletonSource
{
    private IServiceProvider? _parentProvider;

    /// <summary>
    /// The root parent service provider.
    /// </summary>
    public IServiceProvider ParentProvider => _parentProvider
        ?? throw new InvalidOperationException("No parent provider was specified");

    public void SetProvider(
        IServiceProvider parentProvider)
    {
        _parentProvider = parentProvider;
    }
}
