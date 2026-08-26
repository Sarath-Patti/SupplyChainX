using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Application.Common.Interfaces;

public interface IPasswordService
{
    string HashPassword(User user, string password);
    bool VerifyPassword(User user, string passwordHash, string providedPassword);
}
