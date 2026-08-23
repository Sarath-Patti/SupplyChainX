using Microsoft.AspNetCore.Mvc;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;

namespace SupplyChainX.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;

    public InventoryController(IInventoryService inventoryService)
    {
        _inventoryService = inventoryService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<InventoryDto>>> GetInventory(
        [FromQuery] PaginationParams paginationParams,
        [FromQuery] Guid? productId,
        [FromQuery] Guid? warehouseId,
        CancellationToken cancellationToken)
    {
        var result = await _inventoryService.GetInventoryAsync(paginationParams, productId, warehouseId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{productId:guid}/{warehouseId:guid}")]
    public async Task<ActionResult<InventoryDto>> GetInventoryByProductAndWarehouse(
        Guid productId,
        Guid warehouseId,
        CancellationToken cancellationToken)
    {
        var inventory = await _inventoryService.GetInventoryByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);
        return Ok(inventory);
    }

    [HttpPost("adjust")]
    public async Task<ActionResult<InventoryDto>> AdjustInventory(
        [FromBody] AdjustInventoryRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _inventoryService.AdjustInventoryAsync(request, cancellationToken);
        return Ok(updated);
    }
}
