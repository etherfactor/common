using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common.Services;

internal class UnitOfWorkAccessor : IUnitOfWorkAccessor
{
    private static readonly AsyncLocal<HashSet<IUnitOfWork>> _current = new();

    public IUnitOfWork? Current
    {
        get
        {
            var set = _current.Value;
            if (set is null)
                return null;

            lock (set)
            {
                return set.Count switch
                {
                    0 => null,
                    1 => set.Single(),
                    _ => throw new InvalidOperationException(
                        "More than one ambient unit of work scope is active. Attempting to join the ambient scope is ambiguous.")
                };
            }
        }
    }

    public IDisposable Enter(IUnitOfWork uow)
    {
        _current.Value ??= [];
        var set = _current.Value;

        lock (set)
        {
            if (!set.Add(uow))
                throw new InvalidOperationException("Already in an ambient scope for this unit of work.");
        }

        return new DelegateDisposable(() =>
        {
            lock (set)
            {
                set.Remove(uow);
            }
        });
    }
}
