namespace SupplyChainX.Application.Common.Interfaces;

/// <summary>
/// Abstraction for publishing domain events asynchronously to messaging infrastructure.
/// Application layer remains completely decoupled from Kafka implementation details.
/// </summary>
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(string topic, string key, TEvent @event, CancellationToken cancellationToken = default)
        where TEvent : class;
}
