using FluentAssertions;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;
using Xunit;

namespace SupplyChainX.UnitTests.Domain;

public class InventoryDomainTests
{
    private readonly Guid _productId = Guid.NewGuid();
    private readonly Guid _warehouseId = Guid.NewGuid();

    [Fact]
    public void IncreaseStock_WithValidQuantity_ShouldIncreaseAvailableStockAndVersion()
    {
        // Arrange
        var inventory = new Inventory(_productId, _warehouseId, initialAvailable: 100);
        var initialVersion = inventory.Version;

        // Act
        inventory.IncreaseStock(50);

        // Assert
        inventory.AvailableQuantity.Should().Be(150);
        inventory.Version.Should().Be(initialVersion + 1);
    }

    [Fact]
    public void DecreaseStock_WithSufficientStock_ShouldDecreaseAvailableStock()
    {
        // Arrange
        var inventory = new Inventory(_productId, _warehouseId, initialAvailable: 100);

        // Act
        inventory.DecreaseStock(40);

        // Assert
        inventory.AvailableQuantity.Should().Be(60);
    }

    [Fact]
    public void DecreaseStock_WithInsufficientStock_ShouldThrowDomainException()
    {
        // Arrange
        var inventory = new Inventory(_productId, _warehouseId, initialAvailable: 20);

        // Act
        Action act = () => inventory.DecreaseStock(50);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Insufficient available stock*");
    }

    [Fact]
    public void ReserveStock_WithinAvailableStock_ShouldIncreaseReservedQuantity()
    {
        // Arrange
        var inventory = new Inventory(_productId, _warehouseId, initialAvailable: 100);

        // Act
        inventory.ReserveStock(30);

        // Assert
        inventory.ReservedQuantity.Should().Be(30);
    }

    [Fact]
    public void ReserveStock_ExceedingAvailableStock_ShouldThrowDomainException()
    {
        // Arrange
        var inventory = new Inventory(_productId, _warehouseId, initialAvailable: 50);

        // Act
        Action act = () => inventory.ReserveStock(60);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Cannot reserve stock exceeding available stock*");
    }

    [Fact]
    public void CreateInventory_WithEmptyProductId_ShouldThrowDomainException()
    {
        // Act
        Action act = () => new Inventory(Guid.Empty, _warehouseId, 10);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*valid ProductId*");
    }

    [Fact]
    public void CreateInventory_WithEmptyWarehouseId_ShouldThrowDomainException()
    {
        // Act
        Action act = () => new Inventory(_productId, Guid.Empty, 10);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*valid WarehouseId*");
    }
}
