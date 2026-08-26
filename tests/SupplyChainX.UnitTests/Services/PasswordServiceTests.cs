using FluentAssertions;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Infrastructure.Services;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class PasswordServiceTests
{
    private readonly PasswordService _passwordService = new();

    [Fact]
    public void HashPassword_ShouldReturnNonEmptyHashedString()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "dummy_hash");
        var plainPassword = "SecretPassword123!";

        // Act
        var hash = _passwordService.HashPassword(user, plainPassword);

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe(plainPassword);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "dummy_hash");
        var plainPassword = "SecretPassword123!";
        var hash = _passwordService.HashPassword(user, plainPassword);

        // Act
        var isVerified = _passwordService.VerifyPassword(user, hash, plainPassword);

        // Assert
        isVerified.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var user = new User("testuser", "test@example.com", "dummy_hash");
        var plainPassword = "SecretPassword123!";
        var hash = _passwordService.HashPassword(user, plainPassword);

        // Act
        var isVerified = _passwordService.VerifyPassword(user, hash, "WrongPassword");

        // Assert
        isVerified.Should().BeFalse();
    }
}
