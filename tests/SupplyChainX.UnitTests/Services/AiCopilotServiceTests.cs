using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Infrastructure.Services.Ai;
using SupplyChainX.Infrastructure.Services.Ai.Plugins;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class AiCopilotServiceTests
{
    private readonly IProductService _productServiceMock;
    private readonly IWarehouseService _warehouseServiceMock;
    private readonly IInventoryService _inventoryServiceMock;
    private readonly SupplyChainPlugin _plugin;
    private readonly IOptions<AiOptions> _optionsMock;
    private readonly ILogger<AiCopilotService> _loggerMock;
    private readonly AiCopilotService _service;

    public AiCopilotServiceTests()
    {
        _productServiceMock = Substitute.For<IProductService>();
        _warehouseServiceMock = Substitute.For<IWarehouseService>();
        _inventoryServiceMock = Substitute.For<IInventoryService>();
        _plugin = new SupplyChainPlugin(_productServiceMock, _warehouseServiceMock, _inventoryServiceMock);
        _optionsMock = Options.Create(new AiOptions { Provider = "Local" });
        _loggerMock = Substitute.For<ILogger<AiCopilotService>>();

        _service = new AiCopilotService(_optionsMock, _plugin, _loggerMock);
    }

    [Fact]
    public async Task ChatAsync_WithUnauthenticatedUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        var unauthenticatedUser = new ClaimsPrincipal(new ClaimsIdentity());

        // Act & Assert
        await FluentActions.Invoking(() => _service.ChatAsync(new ChatRequest("Hello"), unauthenticatedUser))
            .Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ChatAsync_WithEmptyMessage_ShouldThrowArgumentException()
    {
        // Arrange
        var authenticatedUser = CreateUser("testuser", "Viewer");

        // Act & Assert
        await FluentActions.Invoking(() => _service.ChatAsync(new ChatRequest("  "), authenticatedUser))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task ChatAsync_WithLowStockQuery_ShouldInvokeLowStockToolAndReturnGroundedAnswer()
    {
        // Arrange
        var authenticatedUser = CreateUser("viewer_user", "Viewer");
        var lowStockInventory = new List<InventoryDto>
        {
            new InventoryDto(
                Id: Guid.NewGuid(),
                ProductId: Guid.NewGuid(),
                ProductSku: "SKU-ALERT",
                ProductName: "Critical Product",
                WarehouseId: Guid.NewGuid(),
                WarehouseName: "Main Warehouse",
                AvailableQuantity: 2,
                ReservedQuantity: 5,
                MinimumStockThreshold: 10,
                Version: 1,
                CreatedAtUtc: DateTime.UtcNow,
                UpdatedAtUtc: null
            )
        };

        _inventoryServiceMock
            .GetInventoryAsync(Arg.Any<PaginationParams>(), Arg.Any<Guid?>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<InventoryDto>(lowStockInventory, 1, 100, 1));

        _warehouseServiceMock
            .GetWarehousesAsync(Arg.Any<PaginationParams>(), Arg.Any<bool?>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<WarehouseDto>(new List<WarehouseDto>
            {
                new WarehouseDto(Guid.NewGuid(), "Main Warehouse", "Building 1", true, DateTime.UtcNow, null)
            }, 1, 100, 1));

        // Act
        var result = await _service.ChatAsync(new ChatRequest("Which products are low in stock?"), authenticatedUser);

        // Assert
        result.Should().NotBeNull();
        result.ToolsInvoked.Should().Contain("GetLowStockItemsAsync");
        result.ActivityTrace.Should().NotBeNullOrEmpty();
        result.Response.Should().Contain("Critical Product");
        result.Response.Should().Contain("SKU-ALERT");
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
