using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Schema;

namespace EtherGizmos.Common;

public static class NotificationMetadata
{
    private static readonly ConcurrentDictionary<string, NotificationChannelMetadata> _channelConfig = [];
    private static readonly ConcurrentDictionary<string, NotificationScheduleMetadata> _scheduleConfig = [];

    public static void RegisterChannel(
        string channelKey,
        string displayName,
        Type configType)
    {
        var schema = JsonSchemaExporter
            .GetJsonSchemaAsNode(JsonSerializerOptions.Web, configType, new()
            {
                TreatNullObliviousAsNonNullable = true,
            })
            .ToJsonString(JsonSerializerOptions.Web);
        var meta = new NotificationChannelMetadata(channelKey, displayName, configType, schema);
        _channelConfig.AddOrUpdate(channelKey, _ => meta, (_, _) => meta);
    }

    public static NotificationChannelMetadata GetChannel(
        string channelKey)
        => _channelConfig[channelKey];

    public static void RegisterSchedule(
        string scheduleKey,
        string displayName,
        Type configType)
    {
        var schema = JsonSchemaExporter
            .GetJsonSchemaAsNode(JsonSerializerOptions.Web, configType, new()
            {
                TreatNullObliviousAsNonNullable = true,
            })
            .ToJsonString(JsonSerializerOptions.Web);
        var meta = new NotificationScheduleMetadata(scheduleKey, displayName, configType, schema);
        _scheduleConfig.AddOrUpdate(scheduleKey, _ => meta, (_, _) => meta);
    }

    public static NotificationScheduleMetadata GetSchedule(
        string scheduleKey)
        => _scheduleConfig[scheduleKey];
}
