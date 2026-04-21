using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Schema;

namespace EtherGizmos.Common;

public static class NotificationRegistry
{
    private static readonly ConcurrentDictionary<string, RegisteredNotificationChannel> _channelConfig = [];
    private static readonly ConcurrentDictionary<string, RegisteredNotificationSchedule> _scheduleConfig = [];

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
        var meta = new RegisteredNotificationChannel(channelKey, displayName, configType, schema);
        _channelConfig.AddOrUpdate(channelKey, _ => meta, (_, _) => meta);
    }

    public static RegisteredNotificationChannel GetChannel(
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
        var meta = new RegisteredNotificationSchedule(scheduleKey, displayName, configType, schema);
        _scheduleConfig.AddOrUpdate(scheduleKey, _ => meta, (_, _) => meta);
    }

    public static RegisteredNotificationSchedule GetSchedule(
        string scheduleKey)
        => _scheduleConfig[scheduleKey];
}
