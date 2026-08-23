using Microsoft.AspNetCore.Mvc;

namespace SupplyChainX.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "SupplyChainX API",
            version = "v0.1.0",
            timestamp = DateTime.UtcNow
        });
    }
}
