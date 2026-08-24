using Microsoft.Extensions.DependencyInjection;
using SupplyChainX.Application.Common.Events;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.EventHandlers;
using SupplyChainX.Application.Services;

namespace SupplyChainX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // 1. Application Domain Services
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IInventoryService, InventoryService>();

        // 2. Domain Event Handlers
        services.AddScoped<IEventHandler<ProductCreatedEvent>, ProductCreatedEventHandler>();
        services.AddScoped<IEventHandler<ProductUpdatedEvent>, ProductUpdatedEventHandler>();
        services.AddScoped<IEventHandler<ProductDeletedEvent>, ProductDeletedEventHandler>();
        services.AddScoped<IEventHandler<WarehouseCreatedEvent>, WarehouseCreatedEventHandler>();
        services.AddScoped<IEventHandler<WarehouseUpdatedEvent>, WarehouseUpdatedEventHandler>();
        services.AddScoped<IEventHandler<WarehouseDeletedEvent>, WarehouseDeletedEventHandler>();
        services.AddScoped<IEventHandler<InventoryAdjustedEvent>, InventoryAdjustedEventHandler>();

        return services;
    }
}
