namespace EtherGizmos.Common.Abstractions;

public sealed record UnitOfWorkCreateOptions
{
    public UnitOfWorkAmbientMode AmbientMode { get; init; } = UnitOfWorkAmbientMode.CreateNewAndSetAmbient;

    public UnitOfWorkScopeMode SccopeMode { get; init; } = UnitOfWorkScopeMode.NewScope;

    public IServiceProvider? ScopeProvider { get; init; }
}
