namespace SupplyChainX.Application.Common.Interfaces;

/// <summary>
/// Application abstraction for event idempotency check and tracking.
/// </summary>
public interface IIdempotencyService
{
    Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default);
    Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default);
}
