using FluentAssertions;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;
using Xunit;

namespace SupplyChainX.UnitTests.Domain;

public class WarehouseDomainTests
{
    [Fact]
    public void CreateWarehouse_WithValidData_ShouldCreateWarehouseInstance()
    {
        // Act
        var warehouse = new Warehouse("Central Distribution Hub", "Austin, TX");

        // Assert
        warehouse.Id.Should().NotBeEmpty();
        warehouse.Name.Should().Be("Central Distribution Hub");
        warehouse.Location.Should().Be("Austin, TX");
        warehouse.IsActive.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateWarehouse_WithEmptyOrNullName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        Action act = () => new Warehouse(invalidName!, "Austin, TX");

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*name is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateWarehouse_WithEmptyOrNullLocation_ShouldThrowDomainException(string? invalidLocation)
    {
        // Act
        Action act = () => new Warehouse("North Hub", invalidLocation!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*location is required*");
    }
}
