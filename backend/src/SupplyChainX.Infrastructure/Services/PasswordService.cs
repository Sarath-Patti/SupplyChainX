using Microsoft.AspNetCore.Identity;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Infrastructure.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string passwordHash, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(user, passwordHash, providedPassword);
        return result != PasswordVerificationResult.Failed;
    }
}
