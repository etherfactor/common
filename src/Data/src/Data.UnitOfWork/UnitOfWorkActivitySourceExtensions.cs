using System.Diagnostics;

namespace EtherGizmos.Common;

public static class UnitOfWorkActivitySourceExtensions
{
    private static ActivitySource Source { get; } = new("EtherGizmos.Common.Data.UnitOfWork");

    extension(ActivitySources)
    {
        public static ActivitySource UnitOfWork => Source;
    }
}
