using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Application.EventHandlers;

public class InventoryAdjustedEventHandler : IEventHandler<InventoryAdjustedEvent>
{
    private readonly ILogger<InventoryAdjustedEventHandler> _logger;

    public InventoryAdjustedEventHandler(ILogger<InventoryAdjustedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(InventoryAdjustedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogInformation(
            "[EventProcessed] InventoryAdjustedEvent received. InventoryId: {InventoryId}, ProductId: {ProductId} ({ProductSku}), WarehouseId: {WarehouseId} ({WarehouseName}), Available: {AvailableQuantity}, Reserved: {ReservedQuantity}, Adjusted: {QuantityAdjusted}, Type: {AdjustmentType}, Version: {Version}",
            @event.InventoryId, @event.ProductId, @event.ProductSku, @event.WarehouseId, @event.WarehouseName, @event.AvailableQuantity, @event.ReservedQuantity, @event.QuantityAdjusted, @event.AdjustmentType, @event.Version);

        return Task.CompletedTask;
    }
}
