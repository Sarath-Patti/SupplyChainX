using FluentAssertions;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;
using Xunit;

namespace SupplyChainX.UnitTests.Domain;

public class ProductDomainTests
{
    [Fact]
    public void CreateProduct_WithValidData_ShouldCreateProductInstance()
    {
        // Act
        var product = new Product("SKU-1001", "Gaming Monitor", "27-inch 4K OLED", 499.99m);

        // Assert
        product.Id.Should().NotBeEmpty();
        product.Sku.Should().Be("SKU-1001");
        product.Name.Should().Be("Gaming Monitor");
        product.Description.Should().Be("27-inch 4K OLED");
        product.UnitPrice.Should().Be(499.99m);
        product.IsActive.Should().BeTrue();
        product.CreatedAtUtc.Should().BeBefore(DateTime.UtcNow.AddSeconds(1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateProduct_WithEmptyOrNullSku_ShouldThrowDomainException(string? invalidSku)
    {
        // Act
        Action act = () => new Product(invalidSku!, "Widget", "Desc", 10.00m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*SKU is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateProduct_WithEmptyOrNullName_ShouldThrowDomainException(string? invalidName)
    {
        // Act
        Action act = () => new Product("SKU-9999", invalidName!, "Desc", 10.00m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*name is required*");
    }

    [Fact]
    public void CreateProduct_WithNegativeUnitPrice_ShouldThrowDomainException()
    {
        // Act
        Action act = () => new Product("SKU-1002", "Keyboard", "Mechanical", -50.00m);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*unit price cannot be negative*");
    }
}
