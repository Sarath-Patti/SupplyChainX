namespace SupplyChainX.Application.Common.Interfaces;

/// <summary>
/// Application abstraction for handling domain events asynchronously.
/// </summary>
/// <typeparam name="TEvent">The strongly typed domain event contract.</typeparam>
public interface IEventHandler<in TEvent> where TEvent : class
{
    Task HandleAsync(TEvent @event, CancellationToken cancellationToken = default);
}
