using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Infrastructure.Services.Ai.Plugins;
using SupplyChainX.Infrastructure.Services.Mcp;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class McpServerServiceTests
{
    private readonly IProductService _productServiceMock;
    private readonly IWarehouseService _warehouseServiceMock;
    private readonly IInventoryService _inventoryServiceMock;
    private readonly SupplyChainPlugin _plugin;
    private readonly ILogger<McpServerService> _loggerMock;
    private readonly McpServerService _service;

    public McpServerServiceTests()
    {
        _productServiceMock = Substitute.For<IProductService>();
        _warehouseServiceMock = Substitute.For<IWarehouseService>();
        _inventoryServiceMock = Substitute.For<IInventoryService>();
        _plugin = new SupplyChainPlugin(_productServiceMock, _warehouseServiceMock, _inventoryServiceMock);
        _loggerMock = Substitute.For<ILogger<McpServerService>>();

        _service = new McpServerService(_plugin, _loggerMock);
    }

    [Fact]
    public void ListTools_ShouldReturnExposedMcpTools()
    {
        // Act
        var result = _service.ListTools();

        // Assert
        result.Tools.Should().NotBeEmpty();
        result.Tools.Select(t => t.Name).Should().Contain(new[]
        {
            "supplychainx_get_products",
            "supplychainx_get_warehouses",
            "supplychainx_get_inventory",
            "supplychainx_get_low_stock"
        });
    }

    [Fact]
    public async Task CallToolAsync_WithUnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Act & Assert
        await FluentActions.Invoking(() => _service.CallToolAsync(new McpToolCallRequest("supplychainx_get_products"), unauthenticatedUser))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CallToolAsync_WithLowStockTool_ShouldExecuteToolAndReturnJsonContent()
    {
        // Arrange
        var user = CreateUser("viewer_mcp", "Viewer");
        var lowStockInventory = new List<InventoryDto>
        {
            new InventoryDto(
                Id: Guid.NewGuid(),
                ProductId: Guid.NewGuid(),
                ProductSku: "SKU-MCP-01",
                ProductName: "MCP Tracked Product",
                WarehouseId: Guid.NewGuid(),
                WarehouseName: "Central Facility",
                AvailableQuantity: 1,
                ReservedQuantity: 0,
                MinimumStockThreshold: 15,
                Version: 1,
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: null
            )
        };

        _inventoryServiceMock
            .GetInventoryAsync(Arg.Any<PaginationParams>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new PagedResult<InventoryDto>(lowStockInventory, 1, 100, 1)));

        // Act
        var response = await _service.CallToolAsync(new McpToolCallRequest("supplychainx_get_low_stock"), user);

        // Assert
        response.Should().NotBeNull();
        response.IsError.Should().BeFalse();
        response.Content.Should().HaveCount(1);
        response.Content[0].Text.Should().Contain("SKU-MCP-01");
        response.Content[0].Text.Should().Contain("MCP Tracked Product");
    }

    private static ClaimsPrincipal CreateUser(string username, string role)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, username),
            new Claim(ClaimTypes.Role, role)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        return new ClaimsPrincipal(identity);
    }
}
