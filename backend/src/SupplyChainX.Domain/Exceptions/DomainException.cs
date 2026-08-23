namespace SupplyChainX.Domain.Exceptions;

/// <summary>
/// Base exception for domain rule and invariant violations.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
