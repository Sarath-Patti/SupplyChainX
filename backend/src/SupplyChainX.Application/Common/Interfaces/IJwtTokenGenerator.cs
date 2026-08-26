using SupplyChainX.Domain.Entities;

namespace SupplyChainX.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IEnumerable<string> roles);
}
