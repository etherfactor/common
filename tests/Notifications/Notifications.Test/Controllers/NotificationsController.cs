using EtherGizmos.Common.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json.Nodes;

namespace Notifications.Test.Controllers;

[Route("notifications")]
public class NotificationsController : ControllerBase
{
    private readonly IOptions<NotificationEventOptions> _eventOptions;

    public NotificationsController(
        IOptions<NotificationEventOptions> eventOptions)
    {
        _eventOptions = eventOptions;
    }

    [HttpGet("meta")]
    public async Task<IActionResult> Meta()
    {
        var options = _eventOptions.Value;

        var channels = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Channel))
            .Distinct()
            .Select(e => new
            {
                channelKey = e.ChannelKey,
                displayName = e.DisplayName,
                configSchema = JsonNode.Parse(e.ConfigSchema),
            })
            .ToList();

        var schedules = options.Metadata.Values
            .SelectMany(e => e.Supports.Select(e => e.Schedule))
            .Distinct()
            .Select(e => new
            {
                scheduleKey = e.ScheduleKey,
                displayName = e.DisplayName,
                configSchema = JsonNode.Parse(e.ConfigSchema),
            })
            .ToList();

        var events = options.Metadata.Values
            .Select(e => new
            {
                eventType = e.EventType,
                displayName = e.DisplayName,
                supports = e.Supports.Select(f => new
                {
                    scheduleKey = f.Schedule.ScheduleKey,
                    channelKey = f.Channel.ChannelKey,
                }).ToList(),
            })
            .ToList();

        return Ok(new
        {
            events = events,
            channels = channels,
            schedules = schedules,
        });
    }
}
