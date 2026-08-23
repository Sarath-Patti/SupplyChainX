using SupplyChainX.Domain.Common;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Domain.Entities;

public class Inventory : Entity<Guid>, IAggregateRoot
{
    public Guid ProductId { get; private set; }
    public Guid WarehouseId { get; private set; }
    public int AvailableQuantity { get; private set; }
    public int ReservedQuantity { get; private set; }
    public int MinimumStockThreshold { get; private set; }
    public uint Version { get; private set; } = 1;

    // Navigation properties for EF Core
    public Product? Product { get; private set; }
    public Warehouse? Warehouse { get; private set; }

    private Inventory() { } // EF Core constructor

    public Inventory(Guid productId, Guid warehouseId, int initialAvailable = 0, int minimumThreshold = 0)
        : base(Guid.NewGuid())
    {
        if (productId == Guid.Empty)
        {
            throw new DomainException("Inventory must reference a valid ProductId.");
        }

        if (warehouseId == Guid.Empty)
        {
            throw new DomainException("Inventory must reference a valid WarehouseId.");
        }

        if (initialAvailable < 0)
        {
            throw new DomainException("Initial available quantity cannot be negative.");
        }

        if (minimumThreshold < 0)
        {
            throw new DomainException("Minimum stock threshold cannot be negative.");
        }

        ProductId = productId;
        WarehouseId = warehouseId;
        AvailableQuantity = initialAvailable;
        ReservedQuantity = 0;
        MinimumStockThreshold = minimumThreshold;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void IncreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity to increase must be positive.");
        }

        AvailableQuantity += quantity;
        IncrementVersion();
    }

    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity to decrease must be positive.");
        }

        if (AvailableQuantity < quantity)
        {
            throw new DomainException($"Insufficient available stock. Available: {AvailableQuantity}, Requested: {quantity}.");
        }

        if (AvailableQuantity - quantity < ReservedQuantity)
        {
            throw new DomainException($"Cannot decrease stock below reserved quantity. Available after decrease: {AvailableQuantity - quantity}, Reserved: {ReservedQuantity}.");
        }

        AvailableQuantity -= quantity;
        IncrementVersion();
    }

    public void ReserveStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity to reserve must be positive.");
        }

        if (ReservedQuantity + quantity > AvailableQuantity)
        {
            throw new DomainException($"Cannot reserve stock exceeding available stock. Available: {AvailableQuantity}, Currently Reserved: {ReservedQuantity}, Requested Reservation: {quantity}.");
        }

        ReservedQuantity += quantity;
        IncrementVersion();
    }

    public void ReleaseReservation(int quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainException("Quantity to release must be positive.");
        }

        if (ReservedQuantity < quantity)
        {
            throw new DomainException($"Cannot release reservation greater than currently reserved stock. Reserved: {ReservedQuantity}, Requested Release: {quantity}.");
        }

        ReservedQuantity -= quantity;
        IncrementVersion();
    }

    public void UpdateMinimumStockThreshold(int threshold)
    {
        if (threshold < 0)
        {
            throw new DomainException("Minimum stock threshold cannot be negative.");
        }

        MinimumStockThreshold = threshold;
        IncrementVersion();
    }

    private void IncrementVersion()
    {
        Version++;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
