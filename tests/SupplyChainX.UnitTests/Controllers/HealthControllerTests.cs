using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using SupplyChainX.Api.Controllers;
using Xunit;

namespace SupplyChainX.UnitTests.Controllers;

public class HealthControllerTests
{
    private readonly HealthCheckService _healthCheckService;
    private readonly HealthController _controller;

    public HealthControllerTests()
    {
        _healthCheckService = Substitute.For<HealthCheckService>();
        _controller = new HealthController(_healthCheckService);
    }

    [Fact]
    public async Task GetStatus_WhenHealthy_ShouldReturn200OkWithChecks()
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new(HealthStatus.Healthy, "PostgreSQL reachable", TimeSpan.FromMilliseconds(5), null, null),
            ["kafka"] = new(HealthStatus.Healthy, "Kafka broker reachable", TimeSpan.FromMilliseconds(10), null, null)
        };

        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(15));

        _healthCheckService.CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(report));

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetStatus_WhenUnhealthy_ShouldReturn503StatusCode()
    {
        // Arrange
        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["database"] = new(HealthStatus.Healthy, "PostgreSQL reachable", TimeSpan.FromMilliseconds(5), null, null),
            ["kafka"] = new(HealthStatus.Unhealthy, "Kafka broker unreachable", TimeSpan.FromMilliseconds(10), new Exception("Offline"), null)
        };

        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(15));

        _healthCheckService.CheckHealthAsync(Arg.Any<Func<HealthCheckRegistration, bool>>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(report));

        // Act
        var result = await _controller.GetStatus();

        // Assert
        var statusResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusResult.StatusCode.Should().Be(503);
    }

    [Fact]
    public void GetLiveness_ShouldReturn200Ok()
    {
        // Act
        var result = _controller.GetLiveness();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);
    }
}
