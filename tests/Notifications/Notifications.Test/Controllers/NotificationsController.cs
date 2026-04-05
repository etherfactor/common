using EtherGizmos.Common.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Notifications.Test.Controllers;

[Route("notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationCapabilitiesProvider _capabilitiesProvider;

    public NotificationsController(
        INotificationCapabilitiesProvider capabilitiesProvider)
    {
        _capabilitiesProvider = capabilitiesProvider;
    }

    [HttpGet("meta")]
    public async Task<IActionResult> Meta()
    {
        return Ok(_capabilitiesProvider.GetCapabilities());
    }
}
