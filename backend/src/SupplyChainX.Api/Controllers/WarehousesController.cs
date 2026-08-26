using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;

namespace SupplyChainX.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize(Roles = "Admin,Operator,Viewer")]
public class WarehousesController : ControllerBase
{
    private readonly IWarehouseService _warehouseService;

    public WarehousesController(IWarehouseService warehouseService)
    {
        _warehouseService = warehouseService;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResult<WarehouseDto>>> GetWarehouses(
        [FromQuery] PaginationParams paginationParams,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var result = await _warehouseService.GetWarehousesAsync(paginationParams, isActive, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<WarehouseDto>> GetWarehouseById(Guid id, CancellationToken cancellationToken)
    {
        var warehouse = await _warehouseService.GetWarehouseByIdAsync(id, cancellationToken);
        return Ok(warehouse);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<WarehouseDto>> CreateWarehouse([FromBody] CreateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var created = await _warehouseService.CreateWarehouseAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetWarehouseById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<ActionResult<WarehouseDto>> UpdateWarehouse(Guid id, [FromBody] UpdateWarehouseRequest request, CancellationToken cancellationToken)
    {
        var updated = await _warehouseService.UpdateWarehouseAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Operator")]
    public async Task<IActionResult> DeleteWarehouse(Guid id, CancellationToken cancellationToken)
    {
        await _warehouseService.DeleteWarehouseAsync(id, cancellationToken);
        return NoContent();
    }
}
