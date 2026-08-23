using SupplyChainX.Domain.Common;
using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Domain.Entities;

public class Warehouse : Entity<Guid>, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Location { get; private set; } = null!;
    public bool IsActive { get; private set; }

    private Warehouse() { } // EF Core constructor

    public Warehouse(string name, string location, bool isActive = true)
        : base(Guid.NewGuid())
    {
        SetName(name);
        SetLocation(location);
        IsActive = isActive;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public void Update(string name, string location, bool isActive)
    {
        SetName(name);
        SetLocation(location);
        IsActive = isActive;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void SetName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Warehouse name is required and cannot be empty.");
        }

        Name = name.Trim();
    }

    private void SetLocation(string location)
    {
        if (string.IsNullOrWhiteSpace(location))
        {
            throw new DomainException("Warehouse location is required and cannot be empty.");
        }

        Location = location.Trim();
    }
}
