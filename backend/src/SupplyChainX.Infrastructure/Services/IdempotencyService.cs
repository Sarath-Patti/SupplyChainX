using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of IIdempotencyService using PostgreSQL ProcessedEvents table.
/// </summary>
public class IdempotencyService : IIdempotencyService
{
    private readonly ISupplyChainXDbContext _dbContext;
    private readonly ILogger<IdempotencyService> _logger;

    public IdempotencyService(
        ISupplyChainXDbContext dbContext,
        ILogger<IdempotencyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<bool> HasBeenProcessedAsync(Guid eventId, CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
        {
            return false;
        }

        return await _dbContext.ProcessedEvents
            .AnyAsync(e => e.EventId == eventId, cancellationToken);
    }

    public async Task MarkAsProcessedAsync(Guid eventId, string eventType, CancellationToken cancellationToken = default)
    {
        if (eventId == Guid.Empty)
        {
            throw new ArgumentException("EventId cannot be empty.", nameof(eventId));
        }

        var processedEvent = new ProcessedEvent(eventId, eventType);
        _dbContext.ProcessedEvents.Add(processedEvent);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Recorded event {EventId} ({EventType}) as processed in PostgreSQL.", eventId, eventType);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Event {EventId} was already recorded as processed concurrently.", eventId);
        }
    }
}
