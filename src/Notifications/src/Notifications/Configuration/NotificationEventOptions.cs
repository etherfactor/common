using System.Collections.Concurrent;

namespace EtherGizmos.Common.Configuration;

public class NotificationEventOptions
{
    /// <summary>
    /// Mapping from event type to its metadata.
    /// </summary>
    public ConcurrentDictionary<string, RegisteredNotificationEvent> Metadata { get; } = [];
}
