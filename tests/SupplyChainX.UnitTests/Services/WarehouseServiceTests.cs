using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;
using SupplyChainX.Infrastructure.Persistence;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class WarehouseServiceTests : IDisposable
{
    private readonly SupplyChainXDbContext _dbContext;
    private readonly WarehouseService _service;

    public WarehouseServiceTests()
    {
        var options = new DbContextOptionsBuilder<SupplyChainXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new SupplyChainXDbContext(options);
        _service = new WarehouseService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateWarehouseAsync_WithValidData_ShouldPersistWarehouse()
    {
        // Arrange
        var request = new CreateWarehouseRequest("Central Hub", "Denver, CO");

        // Act
        var result = await _service.CreateWarehouseAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Name.Should().Be("Central Hub");
        result.Location.Should().Be("Denver, CO");

        var dbWarehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == result.Id);
        dbWarehouse.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteWarehouseAsync_WithoutInventory_ShouldDeleteWarehouseSuccessfully()
    {
        // Arrange
        var createResult = await _service.CreateWarehouseAsync(new CreateWarehouseRequest("Empty Hub", "Seattle, WA"));

        // Act
        await _service.DeleteWarehouseAsync(createResult.Id);

        // Assert
        var dbWarehouse = await _dbContext.Warehouses.FirstOrDefaultAsync(w => w.Id == createResult.Id);
        dbWarehouse.Should().BeNull();
    }

    [Fact]
    public async Task DeleteWarehouseAsync_WithExistingInventory_ShouldThrowConflictException()
    {
        // Arrange
        var product = new Product("SKU-WH1", "Test Product", "Desc", 10.00m);
        var warehouse = new Warehouse("Occupied Hub", "Miami, FL");
        _dbContext.Products.Add(product);
        _dbContext.Warehouses.Add(warehouse);

        var inventory = new Inventory(product.Id, warehouse.Id, initialAvailable: 100);
        _dbContext.Inventories.Add(inventory);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.DeleteWarehouseAsync(warehouse.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*associated inventory records exist*");
    }
}
