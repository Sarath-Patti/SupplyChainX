using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.Extensions.Options;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Infrastructure.Services;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class JwtTokenGeneratorTests
{
    [Fact]
    public void GenerateToken_ShouldIncludeUserAndRoleClaims()
    {
        // Arrange
        var jwtOptions = Options.Create(new JwtOptions
        {
            SecretKey = "SuperSecretKeyForTestingJwtTokens123456!",
            Issuer = "SupplyChainXTest",
            Audience = "SupplyChainXTestAudience",
            ExpiryMinutes = 60
        });

        var tokenGenerator = new JwtTokenGenerator(jwtOptions);
        var user = new User("adminuser", "admin@example.com", "hashed_pass");
        var roles = new[] { Role.Admin, Role.Operator };

        // Act
        var tokenString = tokenGenerator.GenerateToken(user, roles);

        // Assert
        tokenString.Should().NotBeNullOrEmpty();

        var handler = new JwtSecurityTokenHandler();
        var jsonToken = handler.ReadJwtToken(tokenString);

        jsonToken.Issuer.Should().Be("SupplyChainXTest");
        jsonToken.Audiences.Should().Contain("SupplyChainXTestAudience");

        var claims = jsonToken.Claims.ToList();
        claims.Should().Contain(c => c.Type == ClaimTypes.NameIdentifier && c.Value == user.Id.ToString());
        claims.Should().Contain(c => c.Type == ClaimTypes.Name && c.Value == "adminuser");
        claims.Should().Contain(c => c.Type == ClaimTypes.Email && c.Value == "admin@example.com");
        claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).Should().BeEquivalentTo(roles);
    }
}
