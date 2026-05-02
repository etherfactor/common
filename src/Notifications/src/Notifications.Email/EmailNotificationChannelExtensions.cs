using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class EmailNotificationChannelExtensions
{
    extension(NotificationChannels)
    {
        public static EmailChannel Email => EmailChannel.Instance;
    }
}
