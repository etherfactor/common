using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Notifications.Test.Controllers;

[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly ILogger _logger;

    public WebhookController(
        ILogger<WebhookController> logger)
    {
        _logger = logger;
    }

    [HttpPost("post")]
    public IActionResult Post(
        [FromBody] JsonDocument payload,
        CancellationToken cancellationToken = default)
    {
        var data = payload.ToString();
        _logger.LogInformation("Received payload: {Data}", data);

        return Accepted();
    }
}
