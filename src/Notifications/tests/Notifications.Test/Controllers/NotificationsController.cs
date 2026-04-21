using EtherGizmos.Common.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Notifications.Test.Controllers;

[Route("notifications")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationCatalogProvider _capabilitiesProvider;

    public NotificationsController(
        INotificationCatalogProvider capabilitiesProvider)
    {
        _capabilitiesProvider = capabilitiesProvider;
    }

    [HttpGet("meta")]
    public async Task<IActionResult> Meta()
    {
        return Ok(_capabilitiesProvider.GetCatalog());
    }
}
