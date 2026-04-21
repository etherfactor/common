using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace EtherGizmos.Common.Services;

internal class NotificationCatalogProvider : INotificationCatalogProvider
{
    private readonly IOptions<NotificationEventOptions> _eventOptions;
    private NotificationCatalog? _capabilities;

    public NotificationCatalogProvider(
        IOptions<NotificationEventOptions> eventOptions)
    {
        _eventOptions = eventOptions;
    }

    public NotificationCatalog GetCatalog()
    {
        _capabilities ??= BuildCapabilities();
        return _capabilities;
    }

    private NotificationCatalog BuildCapabilities()
    {
        var options = _eventOptions.Value;

        var channels = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Channel))
            .Distinct()
            .Select(e => new NotificationCatalogChannel()
            {
                ChannelKey = e.ChannelKey,
                DisplayName = e.DisplayName,
                ConfigSchema = JsonNode.Parse(e.ConfigSchema)!,
            })
            .ToList();

        var schedules = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Schedule))
            .Distinct()
            .Select(e => new NotificationCatalogSchedule()
            {
                ScheduleKey = e.ScheduleKey,
                DisplayName = e.DisplayName,
                ConfigSchema = JsonNode.Parse(e.ConfigSchema)!,
            })
            .ToList();

        var events = options.Metadata.Values
            .Select(e => new NotificationCatalogEvent()
            {
                EventKey = e.EventType,
                DisplayName = e.DisplayName,
                Supports = [.. e.Supports.Select(f => new NotificationCatalogChannelSchedule()
                {
                    ScheduleKey = f.Schedule.ScheduleKey,
                    ChannelKey = f.Channel.ChannelKey,
                })],
            })
            .ToList();

        return new NotificationCatalog()
        {
            Events = events,
            Channels = channels,
            Schedules = schedules,
        };
    }
}
