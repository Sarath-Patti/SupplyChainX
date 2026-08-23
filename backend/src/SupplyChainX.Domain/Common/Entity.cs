namespace SupplyChainX.Domain.Common;

/// <summary>
/// Abstract base class for domain entities.
/// </summary>
/// <typeparam name="TId">The type of entity identifier.</typeparam>
public abstract class Entity<TId>
{
    public TId Id { get; protected set; } = default!;

    public DateTime CreatedAtUtc { get; protected set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; protected set; }

    protected Entity() { }

    protected Entity(TId id)
    {
        Id = id;
    }
}
