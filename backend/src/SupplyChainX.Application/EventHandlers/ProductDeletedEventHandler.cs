using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;

namespace SupplyChainX.Application.EventHandlers;

public class ProductDeletedEventHandler : IEventHandler<ProductDeletedEvent>
{
    private readonly ILogger<ProductDeletedEventHandler> _logger;

    public ProductDeletedEventHandler(ILogger<ProductDeletedEventHandler> logger)
    {
        _logger = logger;
    }

    public Task HandleAsync(ProductDeletedEvent @event, CancellationToken cancellationToken = default)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }

        _logger.LogInformation(
            "[EventProcessed] ProductDeletedEvent received. ProductId: {ProductId}, SKU: {Sku}",
            @event.ProductId, @event.Sku);

        return Task.CompletedTask;
    }
}
