using System.ComponentModel;
using Microsoft.SemanticKernel;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;

namespace SupplyChainX.Infrastructure.Services.Ai.Plugins;

public class SupplyChainPlugin
{
    private readonly IProductService _productService;
    private readonly IWarehouseService _warehouseService;
    private readonly IInventoryService _inventoryService;

    public SupplyChainPlugin(
        IProductService productService,
        IWarehouseService warehouseService,
        IInventoryService inventoryService)
    {
        _productService = productService;
        _warehouseService = warehouseService;
        _inventoryService = inventoryService;
    }

    [KernelFunction, Description("Gets a list of products in the catalog with pagination.")]
    public async Task<List<ProductDto>> GetProductsAsync(
        [Description("Page number, default is 1")] int page = 1,
        [Description("Page size, default is 50")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _productService.GetProductsAsync(new PaginationParams { Page = page, PageSize = pageSize }, true, cancellationToken);
        return result.Items.ToList();
    }

    [KernelFunction, Description("Gets a specific product by its ID or SKU code.")]
    public async Task<ProductDto?> GetProductByIdOrSkuAsync(
        [Description("The Product ID or SKU code (e.g. SKU-001)")] string identifier,
        CancellationToken cancellationToken = default)
    {
        if (Guid.TryParse(identifier, out var id))
        {
            try
            {
                return await _productService.GetProductByIdAsync(id, cancellationToken);
            }
            catch
            {
                // Fallthrough to search by SKU
            }
        }

        var products = await _productService.GetProductsAsync(new PaginationParams { Page = 1, PageSize = 100 }, null, cancellationToken);
        return products.Items.FirstOrDefault(p => p.Sku.Equals(identifier, StringComparison.OrdinalIgnoreCase) || p.Id.ToString() == identifier);
    }

    [KernelFunction, Description("Gets a list of active warehouses in the supply chain network.")]
    public async Task<List<WarehouseDto>> GetWarehousesAsync(
        [Description("Page number, default is 1")] int page = 1,
        [Description("Page size, default is 50")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _warehouseService.GetWarehousesAsync(new PaginationParams { Page = page, PageSize = pageSize }, true, cancellationToken);
        return result.Items.ToList();
    }

    [KernelFunction, Description("Gets inventory records across warehouses with details of available and reserved stock.")]
    public async Task<List<InventoryDto>> GetInventoryAsync(
        [Description("Page number, default is 1")] int page = 1,
        [Description("Page size, default is 50")] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetInventoryAsync(new PaginationParams { Page = page, PageSize = pageSize }, null, null, cancellationToken);
        return result.Items.ToList();
    }

    [KernelFunction, Description("Gets inventory items that are currently low in stock (where available stock is at or below minimum stock threshold).")]
    public async Task<List<InventoryDto>> GetLowStockItemsAsync(
        CancellationToken cancellationToken = default)
    {
        var result = await _inventoryService.GetInventoryAsync(new PaginationParams { Page = 1, PageSize = 100 }, null, null, cancellationToken);
        return result.Items.Where(i => i.AvailableQuantity <= i.MinimumStockThreshold).ToList();
    }
}
