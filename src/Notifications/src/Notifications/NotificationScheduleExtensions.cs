using EtherGizmos.Common.Abstractions;

namespace EtherGizmos.Common;

public static class NotificationScheduleExtensions
{
    extension(NotificationSchedules)
    {
        public static DigestSchedule Digest => DigestSchedule.Instance;

        public static ImmediateSchedule Immediate => ImmediateSchedule.Instance;
    }
}
