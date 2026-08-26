using SupplyChainX.Domain.Exceptions;

namespace SupplyChainX.Domain.Entities;

public class Role
{
    public const string Admin = "Admin";
    public const string Operator = "Operator";
    public const string Viewer = "Viewer";

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string? Description { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = new List<UserRole>();

    private Role()
    {
        Name = null!;
    }

    public Role(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Role name cannot be empty.");

        Id = Guid.NewGuid();
        Name = name.Trim();
        Description = description?.Trim();
    }
}
