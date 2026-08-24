using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Infrastructure.Messaging.Kafka;

/// <summary>
/// Infrastructure implementation of IEventPublisher using Confluent.Kafka producer.
/// Serializes domain events to JSON and publishes them to configured Kafka topics asynchronously.
/// </summary>
public class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;

    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public KafkaEventPublisher(
        IProducer<string, string> producer,
        ILogger<KafkaEventPublisher> logger)
    {
        _producer = producer;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(
        string topic,
        string key,
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : class
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic name cannot be empty.", nameof(topic));
        }

        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        var eventType = typeof(TEvent).Name;
        var jsonPayload = JsonSerializer.Serialize(@event, JsonSerializerOptions);

        var message = new Message<string, string>
        {
            Key = key ?? string.Empty,
            Value = jsonPayload
        };

        try
        {
            _logger.LogInformation("Publishing event {EventType} to Kafka topic {Topic} with key {Key}",
                eventType, topic, key);

            var result = await _producer.ProduceAsync(topic, message, cancellationToken);

            _logger.LogInformation(
                "Successfully published {EventType} to Kafka topic {Topic} [Partition {Partition} @ Offset {Offset}]",
                eventType, result.Topic, result.Partition.Value, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} to Kafka topic {Topic} with key {Key}. Error: {Reason}",
                eventType, topic, key, ex.Error.Reason);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error publishing event {EventType} to Kafka topic {Topic}",
                eventType, topic);
            throw;
        }
    }
}
