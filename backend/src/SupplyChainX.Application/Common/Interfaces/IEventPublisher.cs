namespace SupplyChainX.Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing domain events asynchronously to messaging infrastructure.
/// Supports both typed domain event serialization and raw payload/DLQ publishing with metadata headers.
/// Application layer remains completely decoupled from Kafka implementation details.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(
        string topic,
        string key,
        TEvent @event,
        CancellationToken cancellationToken = default) where TEvent : class;

    Task PublishRawAsync(
        string topic,
        string key,
        string rawJson,
        IDictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default);
}
