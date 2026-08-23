using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Domain.Exceptions;
using SupplyChainX.Infrastructure.Persistence;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class ProductServiceTests : IDisposable
{
    private readonly SupplyChainXDbContext _dbContext;
    private readonly ProductService _service;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<SupplyChainXDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new SupplyChainXDbContext(options);
        _service = new ProductService(_dbContext);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task CreateProductAsync_WithUniqueSku_ShouldPersistProduct()
    {
        // Arrange
        var request = new CreateProductRequest("SKU-ABC", "Test Widget", "Desc", 25.00m);

        // Act
        var result = await _service.CreateProductAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Sku.Should().Be("SKU-ABC");
        result.Name.Should().Be("Test Widget");

        var dbProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == result.Id);
        dbProduct.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateProductAsync_WithDuplicateSku_ShouldThrowConflictException()
    {
        // Arrange
        var request1 = new CreateProductRequest("SKU-DUP", "Widget 1", "Desc", 10.00m);
        var request2 = new CreateProductRequest("sku-dup", "Widget 2", "Desc", 15.00m);

        await _service.CreateProductAsync(request1);

        // Act
        Func<Task> act = async () => await _service.CreateProductAsync(request2);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public async Task GetProductByIdAsync_WithNonExistentId_ShouldThrowNotFoundException()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        Func<Task> act = async () => await _service.GetProductByIdAsync(nonExistentId);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>()
            .WithMessage($"*Product*({nonExistentId})*was not found*");
    }

    [Fact]
    public async Task DeleteProductAsync_WithoutInventory_ShouldDeleteProductSuccessfully()
    {
        // Arrange
        var createResult = await _service.CreateProductAsync(new CreateProductRequest("SKU-DEL1", "To Delete", "Desc", 10.00m));

        // Act
        await _service.DeleteProductAsync(createResult.Id);

        // Assert
        var dbProduct = await _dbContext.Products.FirstOrDefaultAsync(p => p.Id == createResult.Id);
        dbProduct.Should().BeNull();
    }

    [Fact]
    public async Task DeleteProductAsync_WithExistingInventory_ShouldThrowConflictException()
    {
        // Arrange
        var product = new Product("SKU-DEL2", "With Inventory", "Desc", 15.00m);
        var warehouse = new Warehouse("Hub A", "Location A");
        _dbContext.Products.Add(product);
        _dbContext.Warehouses.Add(warehouse);

        var inventory = new Inventory(product.Id, warehouse.Id, initialAvailable: 50);
        _dbContext.Inventories.Add(inventory);
        await _dbContext.SaveChangesAsync();

        // Act
        Func<Task> act = async () => await _service.DeleteProductAsync(product.Id);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*associated inventory records exist*");
    }
}
