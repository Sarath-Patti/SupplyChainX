using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Application.EventHandlers;

public class ProductUpdatedEventHandler : IEventHandler<ProductUpdatedEvent>
{
    private readonly ILogger<ProductUpdatedEventHandler> _logger;

    public ProductUpdatedEventHandler(ILogger<ProductUpdatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ProductUpdatedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogInformation(
            "[EventProcessed] ProductUpdatedEvent received. ProductId: {ProductId}, SKU: {Sku}, Name: {Name}, UnitPrice: {UnitPrice:C}, IsActive: {IsActive}",
            @event.ProductId, @event.Sku, @event.Name, @event.UnitPrice, @event.IsActive);

        return Task.CompletedTask;
    }
}
