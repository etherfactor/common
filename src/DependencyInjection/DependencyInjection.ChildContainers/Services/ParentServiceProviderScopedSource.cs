namespace EtherGizmos.Common.Services;

/// <summary>
/// Contains a reference to the current scoped parent service provider.
/// </summary>
internal class ParentServiceProviderScopedSource
{
    private IServiceProvider? _parentProvider;

    /// <summary>
    /// The scoped parent service provider.
    /// </summary>
    public IServiceProvider? ParentProvider => _parentProvider;

    public void SetProvider(
        IServiceProvider parentProvider)
    {
        _parentProvider = parentProvider;
    }
}
