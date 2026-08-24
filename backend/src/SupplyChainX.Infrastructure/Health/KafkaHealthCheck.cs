using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace SupplyChainX.Infrastructure.Health;

/// <summary>
/// Active health check that performs real connectivity checks against the configured Kafka broker cluster.
/// Returns Healthy if metadata query succeeds, or Unhealthy if Kafka is unreachable.
/// </summary>
public class KafkaHealthCheck : IHealthCheck
{
    private readonly string _bootstrapServers;
    private readonly ILogger<KafkaHealthCheck> _logger;

    public KafkaHealthCheck(
        IConfiguration configuration,
        ILogger<KafkaHealthCheck> logger)
    {
        _bootstrapServers = configuration["Kafka:BootstrapServers"]
            ?? Environment.GetEnvironmentVariable("KAFKA_BOOTSTRAP_SERVERS")
            ?? "localhost:9092";
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var config = new AdminClientConfig
            {
                BootstrapServers = _bootstrapServers,
                SocketTimeoutMs = 3000
            };

            using var adminClient = new AdminClientBuilder(config).Build();
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(3));

            if (metadata != null && metadata.Brokers.Count > 0)
            {
                var brokerCount = metadata.Brokers.Count;
                var topicCount = metadata.Topics.Count;

                return Task.FromResult(HealthCheckResult.Healthy(
                    $"Kafka broker connection healthy. Reachable brokers: {brokerCount}, Topics: {topicCount}"));
            }

            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Kafka metadata query returned no active brokers on {_bootstrapServers}."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[KafkaHealthCheckFailed] Failed to connect to Kafka broker on {BootstrapServers}", _bootstrapServers);

            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Kafka broker unreachable on {_bootstrapServers}. Error: {ex.Message}", ex));
        }
    }
}
