namespace SupplyChainX.Domain.Exceptions;

/// <summary>
/// Exception thrown when a unique constraint or domain conflict occurs.
/// </summary>
public class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
