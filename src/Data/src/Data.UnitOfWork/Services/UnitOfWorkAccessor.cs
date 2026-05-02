using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class UnitOfWorkAccessor : IUnitOfWorkAccessor
{
    private static readonly AsyncLocal<HashSet<IUnitOfWork>> _current = new();

    public IUnitOfWork? Current => _current.Value?.Count > 1
        ? throw new InvalidOperationException("More than one ambient unit of work scope is active. Attempting to join the " +
            "ambient scope is ambiguous.")
        : _current.Value?.SingleOrDefault();

    public IDisposable Enter(IUnitOfWork uow)
    {
        _current.Value ??= [];
        if (!_current.Value.Add(uow))
            throw new InvalidOperationException("Already in an ambient scope for this unit of work.");

        return new DelegateDisposable(() => _current.Value.Remove(uow));
    }
}
