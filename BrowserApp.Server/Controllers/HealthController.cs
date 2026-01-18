using Microsoft.AspNetCore.Mvc;

namespace BrowserApp.Server.Controllers;

/// <summary>
/// Health check controller for monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Health check endpoint.
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });
    }
}
