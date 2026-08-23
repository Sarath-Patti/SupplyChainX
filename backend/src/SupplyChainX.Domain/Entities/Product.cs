using SupplyChainX.Domain.Common;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Domain.Entities;

public class Product : Entity<Guid>, IAggregateRoot
{
    public string Sku { get; private set; } = null!;
    public string Name { get; private set; } = null!;
    public string? Description { get; private set; }
    public decimal UnitPrice { get; private set; }
    public bool IsActive { get; private set; }

    private Product() { } // EF Core constructor

    public Product(string sku, string name, string? description, decimal unitPrice, bool isActive = true)
        : base(Guid.NewGuid())
    {
        SetSku(sku);
        SetName(name);
        SetDescription(description);
        SetUnitPrice(unitPrice);
        IsActive = isActive;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string sku, string name, string? description, decimal unitPrice, bool isActive)
    {
        SetSku(sku);
        SetName(name);
        SetDescription(description);
        SetUnitPrice(unitPrice);
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetSku(string sku)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            throw new DomainException("Product SKU is required and cannot be empty.");
        }

        Sku = sku.Trim().ToUpperInvariant();
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Product name is required and cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetDescription(string? description)
    {
        Description = description?.Trim();
    }

    private void SetUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new DomainException("Product unit price cannot be negative.");
        }

        UnitPrice = unitPrice;
    }
}
