using SupplyChainX.Application.Common.Models;

namespace SupplyChainX.Application.Common.Interfaces;

/// <summary>
/// Service interface for executing repeatable domain event workloads, measuring Kafka consumer lag,
/// testing event-driven backpressure, and validating distributed failure recovery & idempotency semantics.
/// </summary>
public interface IKafkaBenchmarkService
{
    Task<BenchmarkExecutionResultDto> PublishWorkloadAsync(
        BenchmarkWorkloadRequest request,
        CancellationToken cancellationToken = default);

    Task<BenchmarkExecutionResultDto> TriggerBurstWorkloadAsync(
        BenchmarkBurstRequest request,
        CancellationToken cancellationToken = default);

    Task<KafkaLagStatusDto> GetConsumerLagAsync(CancellationToken cancellationToken = default);

    Task<BenchmarkExecutionResultDto> PublishDuplicateEventAsync(CancellationToken cancellationToken = default);

    Task<BenchmarkExecutionResultDto> PublishPoisonEventAsync(CancellationToken cancellationToken = default);
}
