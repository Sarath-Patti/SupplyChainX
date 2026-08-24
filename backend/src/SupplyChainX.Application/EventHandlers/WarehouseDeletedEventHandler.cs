using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Application.EventHandlers;

public class WarehouseDeletedEventHandler : IEventHandler<WarehouseDeletedEvent>
{
    private readonly ILogger<WarehouseDeletedEventHandler> _logger;

    public WarehouseDeletedEventHandler(ILogger<WarehouseDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(WarehouseDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogInformation(
            "[EventProcessed] WarehouseDeletedEvent received. WarehouseId: {WarehouseId}, Name: {Name}",
            @event.WarehouseId, @event.Name);

        return Task.CompletedTask;
    }
}
