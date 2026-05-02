using EtherGizmos.Common.Models;
using System.Text.Json;

namespace EtherGizmos.Common;

public static class NotificationSubscriptionExtensions
{
    extension(NotificationSubscription @this)
    {
        public object ChannelConfig
        {
            get => JsonSerializer.Deserialize(@this.ChannelConfigRaw, NotificationRegistry.GetChannel(@this.ChannelKey).ConfigType, JsonSerializerOptions.Web)!;
            set => @this.ChannelConfigRaw = JsonSerializer.Serialize(value, NotificationRegistry.GetChannel(@this.ChannelKey).ConfigType, JsonSerializerOptions.Web);
        }

        public object ScheduleConfig
        {
            get => JsonSerializer.Deserialize(@this.ScheduleConfigRaw, NotificationRegistry.GetSchedule(@this.ScheduleType).ConfigType, JsonSerializerOptions.Web)!;
            set => @this.ScheduleConfigRaw = JsonSerializer.Serialize(value, NotificationRegistry.GetSchedule(@this.ScheduleType).ConfigType, JsonSerializerOptions.Web);
        }
    }
}
