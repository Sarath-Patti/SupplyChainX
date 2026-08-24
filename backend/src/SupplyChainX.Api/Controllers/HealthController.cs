using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace SupplyChainX.Api.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    /// <summary>
    /// GET /health - Detailed overall health status report for PostgreSQL and Kafka dependencies.
    /// </summary>
    [HttpGet("/health")]
    public async Task<IActionResult> GetStatus()
    {
        var report = await _healthCheckService.CheckHealthAsync();

        var response = new
        {
            status = report.Status.ToString(),
            service = "SupplyChainX API",
            version = "v0.7.0",
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

    /// <summary>
    /// GET /health/ready - Readiness check verifying that PostgreSQL and Kafka dependencies are ready.
    /// </summary>
    [HttpGet("/health/ready")]
    [HttpGet("/health/readiness")]
    public async Task<IActionResult> GetReadiness()
    {
        var report = await _healthCheckService.CheckHealthAsync(r => r.Tags.Contains("ready"));

        var response = new
        {
            status = report.Status.ToString(),
            service = "SupplyChainX API",
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description ?? $"{e.Key} readiness check"
            })
        };

        return report.Status == HealthStatus.Healthy ? Ok(response) : StatusCode(503, response);
    }

    /// <summary>
    /// GET /health/live - Liveness check verifying the API process is alive.
    /// </summary>
    [HttpGet("/health/live")]
    [HttpGet("/health/liveness")]
    public IActionResult GetLiveness()
    {
        var response = new
        {
            status = "Healthy",
            service = "SupplyChainX API",
            timestamp = DateTime.UtcNow
        };

        return Ok(response);
    }
}
