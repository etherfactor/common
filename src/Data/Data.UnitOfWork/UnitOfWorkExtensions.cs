using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Services;

namespace EtherGizmos.Common;

public static class UnitOfWorkExtensions
{
    extension(IUnitOfWorkFactory @this)
    {
        public IUnitOfWorkFactory AsUnfiltered()
        {
            return new UnfilteredUnitOfWorkFactory(@this);
        }
    }
}
