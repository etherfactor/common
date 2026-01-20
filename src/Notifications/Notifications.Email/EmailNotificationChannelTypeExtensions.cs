using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class EmailNotificationChannelTypeExtensions
{
    extension(NotificationChannelType)
    {
        public static string Email => "Email";
    }
}
