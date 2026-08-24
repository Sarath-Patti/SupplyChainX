using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Api.Controllers;

[ApiController]
public class MetricsController : ControllerBase
{
    private readonly IKafkaConsumerStatusService _statusService;
    private static readonly DateTime ProcessStartTimeUtc = DateTime.UtcNow;

    public MetricsController(IKafkaConsumerStatusService statusService)
    {
        _statusService = statusService;
    }

    /// <summary>
    /// GET /api/v1/metrics - Operational metrics and Kafka consumer status endpoint.
    /// Exposes consumer state, throughput counters, retry/DLQ statistics, and system uptime.
    /// Does not expose secrets, credentials, or connection strings.
    /// </summary>
    [HttpGet("/api/v1/metrics")]
    public IActionResult GetMetrics()
    {
        var consumerStatus = _statusService.GetStatus();
        var process = Process.GetCurrentProcess();

        var response = new
        {
            timestamp = DateTime.UtcNow,
            service = "SupplyChainX API",
            version = "v0.7.0",
            consumerStatus = new
            {
                isRunning = consumerStatus.IsRunning,
                consumerGroupId = consumerStatus.ConsumerGroupId,
                subscribedTopics = consumerStatus.SubscribedTopics,
                lastEventConsumedAtUtc = consumerStatus.LastEventConsumedAtUtc,
                lastEventProcessedAtUtc = consumerStatus.LastEventProcessedAtUtc,
                lastProcessingFailureAtUtc = consumerStatus.LastProcessingFailureAtUtc,
                lastProcessingFailureReason = consumerStatus.LastProcessingFailureReason
            },
            metrics = new
            {
                eventsConsumed = consumerStatus.Metrics.ConsumedCount,
                eventsProcessed = consumerStatus.Metrics.ProcessedCount,
                duplicateEventsSkipped = consumerStatus.Metrics.DuplicateCount,
                processingFailures = consumerStatus.Metrics.FailureCount,
                retryAttempts = consumerStatus.Metrics.RetryCount,
                eventsPublishedToDlq = consumerStatus.Metrics.DlqCount,
                malformedEvents = consumerStatus.Metrics.MalformedCount,
                dlqSuccessCount = consumerStatus.Metrics.DlqSuccessCount,
                dlqFailureCount = consumerStatus.Metrics.DlqFailureCount
            },
            system = new
            {
                uptimeSeconds = (long)(DateTime.UtcNow - ProcessStartTimeUtc).TotalSeconds,
                processId = process.Id,
                workingSetBytes = process.WorkingSet64,
                threadCount = process.Threads.Count
            }
        };

        return Ok(response);
    }
}
