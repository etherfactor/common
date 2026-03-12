using EtherGizmos.Common.Models;
using System.Text.Json;

namespace EtherGizmos.Common;

public static class NotificationSubscriptionExtensions
{
    extension(NotificationSubscription @this)
    {
        public object ChannelConfig
        {
            get => JsonSerializer.Deserialize(@this.ChannelConfigRaw, NotificationMetadata.GetChannel(@this.ChannelKey).ConfigType, JsonSerializerOptions.Web)!;
            set => @this.ChannelConfigRaw = JsonSerializer.Serialize(value, NotificationMetadata.GetChannel(@this.ChannelKey).ConfigType, JsonSerializerOptions.Web);
        }

        public object ScheduleConfig
        {
            get => JsonSerializer.Deserialize(@this.ScheduleConfigRaw, NotificationMetadata.GetSchedule(@this.ScheduleType).ConfigType, JsonSerializerOptions.Web)!;
            set => @this.ScheduleConfigRaw = JsonSerializer.Serialize(value, NotificationMetadata.GetSchedule(@this.ScheduleType).ConfigType, JsonSerializerOptions.Web);
        }
    }
}
