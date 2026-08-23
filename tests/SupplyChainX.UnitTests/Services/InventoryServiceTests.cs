using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;
using SupplyChainX.Infrastructure.Persistence;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class InventoryServiceTests : IDisposable
{
    private readonly SupplyChainXDbContext _dbContext;
    private readonly InventoryService _service;
    private readonly Product _product;
    private readonly Warehouse _warehouse;

    public InventoryServiceTests()
    {
        var options = new DbContextOptionsBuilder<SupplyChainXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new SupplyChainXDbContext(options);
        _service = new InventoryService(_dbContext);

        _product = new Product("SKU-TEST", "Test Product", "Desc", 99.99m);
        _warehouse = new Warehouse("Main Warehouse", "Building A");

        _dbContext.Products.Add(_product);
        _dbContext.Warehouses.Add(_warehouse);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task AdjustInventoryAsync_IncreaseStock_ShouldCreateAndIncreaseInventory()
    {
        // Arrange
        var request = new AdjustInventoryRequest(_product.Id, _warehouse.Id, 100, InventoryAdjustmentType.Increase);

        // Act
        var result = await _service.AdjustInventoryAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.AvailableQuantity.Should().Be(100);
        result.ReservedQuantity.Should().Be(0);
        result.ProductId.Should().Be(_product.Id);
        result.WarehouseId.Should().Be(_warehouse.Id);
    }

    [Fact]
    public async Task AdjustInventoryAsync_DecreaseStock_ShouldReduceAvailableStock()
    {
        // Arrange
        await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 100, InventoryAdjustmentType.Increase));

        // Act
        var result = await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 40, InventoryAdjustmentType.Decrease));

        // Assert
        result.AvailableQuantity.Should().Be(60);
    }

    [Fact]
    public async Task AdjustInventoryAsync_DecreaseStockExceedingAvailable_ShouldThrowDomainException()
    {
        // Arrange
        await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 20, InventoryAdjustmentType.Increase));

        // Act
        Func<Task> act = async () => await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 50, InventoryAdjustmentType.Decrease));

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Insufficient available stock*");
    }

    [Fact]
    public async Task AdjustInventoryAsync_ReserveStockExceedingAvailable_ShouldThrowDomainException()
    {
        // Arrange
        await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 30, InventoryAdjustmentType.Increase));

        // Act
        Func<Task> act = async () => await _service.AdjustInventoryAsync(new AdjustInventoryRequest(_product.Id, _warehouse.Id, 50, InventoryAdjustmentType.Reserve));

        // Assert
        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("*Cannot reserve stock exceeding available stock*");
    }

    [Fact]
    public async Task AdjustInventoryAsync_WithInvalidProductId_ShouldThrowNotFoundException()
    {
        // Arrange
        var invalidProductId = Guid.NewGuid();
        var request = new AdjustInventoryRequest(invalidProductId, _warehouse.Id, 10, InventoryAdjustmentType.Increase);

        // Act
        Func<Task> act = async () => await _service.AdjustInventoryAsync(request);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Product*({invalidProductId})*was not found*");
    }
}
