using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using EtherGizmos.Common.Converters;
using EtherGizmos.Common.Models;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EtherGizmos.Common.Services;

internal class NotificationCatalogProvider : INotificationCatalogProvider
{
    private static readonly JsonSerializerOptions _jsonOptions;

    private readonly IOptions<NotificationEventOptions> _eventOptions;
    private NotificationCatalog? _capabilities;

    public NotificationCatalogProvider(
        IOptions<NotificationEventOptions> eventOptions)
    {
        _eventOptions = eventOptions;
    }

    static NotificationCatalogProvider()
    {
        _jsonOptions = new(JsonSerializerOptions.Web)
        {
            Converters =
            {
                new JsonStringEnumConverter(),
                new ObjectToInferredTypesConverter(),
            },
        };
    }

    public NotificationCatalog GetCatalog()
    {
        _capabilities ??= BuildCapabilities();
        return _capabilities;
    }

    private NotificationCatalog BuildCapabilities()
    {
        var options = _eventOptions.Value;
        var now = DateTimeOffset.UtcNow;

        var channels = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Channel))
            .Distinct()
            .Select(e => new NotificationChannel()
            {
                Id = e.ChannelKey,
                Name = e.DisplayName,
                IsAvailable = true,
                LastSeenAt = now,
                ConfigSchema = JsonSerializer.Deserialize<IDictionary<string, object?>>(e.ConfigSchema, _jsonOptions)!,
            })
            .OrderBy(e => e.Id)
            .ToList();

        var schedules = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Schedule))
            .Distinct()
            .Select(e => new NotificationSchedule()
            {
                Id = e.ScheduleKey,
                Name = e.DisplayName,
                IsAvailable = true,
                LastSeenAt = now,
                ConfigSchema = JsonSerializer.Deserialize<IDictionary<string, object?>>(e.ConfigSchema, _jsonOptions)!,
            })
            .OrderBy(e => e.Id)
            .ToList();

        var events = options.Metadata.Values
            .Select(e => new NotificationEvent()
            {
                Id = e.EventKey,
                Name = e.DisplayName,
                IsAvailable = true,
                LastSeenAt = now,
                ConfigSchema = JsonSerializer.Deserialize<IDictionary<string, object?>>(e.ConfigSchema, _jsonOptions)!,
                Supports = [.. e.Supports.Select(f => new NotificationChannelSchedule()
                {
                    EventId = e.EventKey,
                    ScheduleId = f.Schedule.ScheduleKey,
                    ChannelId = f.Channel.ChannelKey,
                }).OrderBy(e => e.ScheduleId).ThenBy(e => e.ChannelId)],
            })
            .OrderBy(e => e.Id)
            .ToList();

        return new NotificationCatalog()
        {
            Events = [.. events],
            Channels = [.. channels],
            Schedules = [.. schedules],
        };
    }
}
