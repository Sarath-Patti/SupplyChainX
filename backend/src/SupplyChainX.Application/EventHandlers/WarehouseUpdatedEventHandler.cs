using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Application.EventHandlers;

public class WarehouseUpdatedEventHandler : IEventHandler<WarehouseUpdatedEvent>
{
    private readonly ILogger<WarehouseUpdatedEventHandler> _logger;

    public WarehouseUpdatedEventHandler(ILogger<WarehouseUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(WarehouseUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogInformation(
            "[EventProcessed] WarehouseUpdatedEvent received. WarehouseId: {WarehouseId}, Name: {Name}, Location: {Location}, IsActive: {IsActive}",
            @event.WarehouseId, @event.Name, @event.Location, @event.IsActive);

        return Task.CompletedTask;
    }
}
