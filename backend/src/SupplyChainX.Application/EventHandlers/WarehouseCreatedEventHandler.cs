using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Application.EventHandlers;

public class WarehouseCreatedEventHandler : IEventHandler<WarehouseCreatedEvent>
{
    private readonly ILogger<WarehouseCreatedEventHandler> _logger;

    public WarehouseCreatedEventHandler(ILogger<WarehouseCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(WarehouseCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogInformation(
            "[EventProcessed] WarehouseCreatedEvent received. WarehouseId: {WarehouseId}, Name: {Name}, Location: {Location}, IsActive: {IsActive}",
            @event.WarehouseId, @event.Name, @event.Location, @event.IsActive);

        return Task.CompletedTask;
    }
}
