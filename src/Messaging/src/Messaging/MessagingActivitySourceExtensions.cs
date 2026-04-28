using System.Diagnostics;

namespace EtherGizmos.Common;

public static class MessagingActivitySourceExtensions
{
    private static ActivitySource Source { get; } = new("EtherGizmos.Common.Messaging");

    extension(ActivitySources)
    {
        public static ActivitySource Messaging => Source;
    }
}
