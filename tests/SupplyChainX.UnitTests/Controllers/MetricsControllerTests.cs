using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SupplyChainX.Api.Controllers;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.Common.Models;
using Xunit;

namespace SupplyChainX.UnitTests.Controllers;

public class MetricsControllerTests
{
    private readonly IKafkaConsumerStatusService _statusService;
    private readonly MetricsController _controller;

    public MetricsControllerTests()
    {
        _statusService = Substitute.For<IKafkaConsumerStatusService>();
        _controller = new MetricsController(_statusService);
    }

    [Fact]
    public void GetMetrics_ShouldReturnStructuredMetricsResponseWithoutSensitiveData()
    {
        // Arrange
        var statusDto = new KafkaConsumerStatusDto
        {
            IsRunning = true,
            ConsumerGroupId = "supplychainx-event-consumers",
            SubscribedTopics = new List<string> { "supplychainx.product.events" },
            Metrics = new KafkaMetricsDto
            {
                ConsumedCount = 10,
                ProcessedCount = 8,
                DuplicateCount = 1,
                FailureCount = 1,
                RetryCount = 3,
                DlqCount = 1,
                MalformedCount = 0,
                DlqSuccessCount = 1,
                DlqFailureCount = 0
            }
        };

        _statusService.GetStatus().Returns(statusDto);

        // Act
        var result = _controller.GetMetrics();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var json = JsonSerializer.Serialize(okResult.Value);
        json.Should().Contain("supplychainx-event-consumers");
        json.Should().Contain("eventsConsumed");
        json.Should().Contain("eventsProcessed");
        json.Should().Contain("duplicateEventsSkipped");
        json.Should().Contain("eventsPublishedToDlq");

        // Verify NO sensitive information is exposed
        json.Should().NotContain("Password");
        json.Should().NotContain("Secret");
        json.Should().NotContain("postgres_dev_password");
        json.Should().NotContain("ConnectionString");
    }
}
