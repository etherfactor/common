using EtherGizmos.Common.Abstractions;
using EtherGizmos.Common.Configuration;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace EtherGizmos.Common.Services;

internal class NotificationCapabilitiesProvider : INotificationCapabilitiesProvider
{
    private readonly IOptions<NotificationEventOptions> _eventOptions;
    private NotificationCapabilities? _capabilities;

    public NotificationCapabilitiesProvider(
        IOptions<NotificationEventOptions> eventOptions)
    {
        _eventOptions = eventOptions;
    }

    public NotificationCapabilities GetCapabilities()
    {
        _capabilities ??= BuildCapabilities();
        return _capabilities;
    }

    private NotificationCapabilities BuildCapabilities()
    {
        var options = _eventOptions.Value;

        var channels = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Channel))
            .Distinct()
            .Select(e => new NotificationChannelCapability()
            {
                ChannelKey = e.ChannelKey,
                DisplayName = e.DisplayName,
                ConfigSchema = JsonNode.Parse(e.ConfigSchema)!,
            })
            .ToList();

        var schedules = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Schedule))
            .Distinct()
            .Select(e => new NotificationScheduleCapability()
            {
                ScheduleKey = e.ScheduleKey,
                DisplayName = e.DisplayName,
                ConfigSchema = JsonNode.Parse(e.ConfigSchema)!,
            })
            .ToList();

        var events = options.Metadata.Values
            .Select(e => new NotificationEventCapability()
            {
                EventKey = e.EventType,
                DisplayName = e.DisplayName,
                Supports = [.. e.Supports.Select(f => new NotificationChannelScheduleCapability()
                {
                    ScheduleKey = f.Schedule.ScheduleKey,
                    ChannelKey = f.Channel.ChannelKey,
                })],
            })
            .ToList();

        return new NotificationCapabilities()
        {
            Events = events,
            Channels = channels,
            Schedules = schedules,
        };
    }
}
