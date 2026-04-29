using System.Diagnostics;

namespace EtherGizmos.Common;

public static class NotificationsActivitySourceExtensions
{
    private static ActivitySource Source { get; } = new("EtherGizmos.Common.Notifications");

    extension(ActivitySources)
    {
        public static ActivitySource Notifications => Source;
    }
}
