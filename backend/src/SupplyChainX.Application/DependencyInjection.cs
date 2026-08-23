using Microsoft.Extensions.DependencyInjection;
using SupplyChainX.Application.Services;

namespace SupplyChainX.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IProductService, ProductService>();
        services.AddScoped<IWarehouseService, WarehouseService>();
        services.AddScoped<IInventoryService, InventoryService>();

        return services;
    }
}
