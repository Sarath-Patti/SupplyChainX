using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Application.EventHandlers;

public class ProductCreatedEventHandler : IEventHandler<ProductCreatedEvent>
{
    private readonly ILogger<ProductCreatedEventHandler> _logger;

    public ProductCreatedEventHandler(ILogger<ProductCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ProductCreatedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogInformation(
            "[EventProcessed] ProductCreatedEvent received. ProductId: {ProductId}, SKU: {Sku}, Name: {Name}, UnitPrice: {UnitPrice:C}, IsActive: {IsActive}",
            @event.ProductId, @event.Sku, @event.Name, @event.UnitPrice, @event.IsActive);

        return Task.CompletedTask;
    }
}
