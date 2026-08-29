using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.Common.Models;

namespace SupplyChainX.Api.Controllers;

[ApiController]
[Authorize]
public class BenchmarkController : ControllerBase
{
    private readonly IKafkaBenchmarkService _benchmarkService;

    public BenchmarkController(IKafkaBenchmarkService benchmarkService)
    {
        _benchmarkService = benchmarkService;
    }

    /// <summary>
    /// POST /api/v1/benchmark/publish - Generates a repeatable domain event workload across Kafka topics.
    /// </summary>
    [HttpPost("/api/v1/benchmark/publish")]
    public async Task<ActionResult<BenchmarkExecutionResultDto>> PublishWorkload(
        [FromBody] BenchmarkWorkloadRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _benchmarkService.PublishWorkloadAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/benchmark/burst - Triggers a high-throughput event burst to test consumer backpressure.
    /// </summary>
    [HttpPost("/api/v1/benchmark/burst")]
    public async Task<ActionResult<BenchmarkExecutionResultDto>> TriggerBurst(
        [FromBody] BenchmarkBurstRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _benchmarkService.TriggerBurstWorkloadAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/benchmark/duplicate - Deliberately publishes duplicate events to test idempotency deduplication.
    /// </summary>
    [HttpPost("/api/v1/benchmark/duplicate")]
    public async Task<ActionResult<BenchmarkExecutionResultDto>> PublishDuplicate(CancellationToken cancellationToken)
    {
        var result = await _benchmarkService.PublishDuplicateEventAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/benchmark/poison - Publishes a poison payload to test retry exhaustion and publication to DLQ.
    /// </summary>
    [HttpPost("/api/v1/benchmark/poison")]
    public async Task<ActionResult<BenchmarkExecutionResultDto>> PublishPoison(CancellationToken cancellationToken)
    {
        var result = await _benchmarkService.PublishPoisonEventAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// GET /api/v1/benchmark/lag - Retrieves current consumer group lag, high watermarks, and committed offsets.
    /// </summary>
    [HttpGet("/api/v1/benchmark/lag")]
    public async Task<ActionResult<KafkaLagStatusDto>> GetConsumerLag(CancellationToken cancellationToken)
    {
        var result = await _benchmarkService.GetConsumerLagAsync(cancellationToken);
        return Ok(result);
    }
}
