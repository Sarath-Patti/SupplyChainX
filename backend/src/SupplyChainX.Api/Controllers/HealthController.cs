using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SupplyChainX.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet]
    public async Task<IActionResult> GetStatus()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = report.Status.ToString(),
            service = "SupplyChainX API",
            version = "v0.2.0",
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? $"{e.Key} reachability check"
            })
        };

        return report.Status == HealthStatus.Healthy ? Ok(response) : StatusCode(503, response);
    }
}
