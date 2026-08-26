using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Domain.Entities;
using SupplyChainX.Domain.Exceptions;
using SupplyChainX.Infrastructure.Persistence;
using SupplyChainX.Infrastructure.Services;
using Xunit;

namespace SupplyChainX.UnitTests.Services;

public class AuthServiceTests
{
    private SupplyChainXDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<SupplyChainXDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new SupplyChainXDbContext(options);
        
        // Seed default roles
        context.Roles.AddRange(
            new Role(Role.Admin, "Admin role"),
            new Role(Role.Operator, "Operator role"),
            new Role(Role.Viewer, "Viewer role")
        );
        context.SaveChanges();

        return context;
    }

    [Fact]
    public async Task RegisterAsync_WithExplicitViewerRole_ShouldCreateUserWithViewerRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var request = new RegisterRequest("viewer_v09_fix", "viewer@example.com", "ViewerPassword123!", "Viewer");

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.User.Roles.Should().ContainSingle(Role.Viewer);

        var userInDb = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == "viewer_v09_fix");

        userInDb.Should().NotBeNull();
        userInDb!.UserRoles.Select(ur => ur.Role.Name).Should().ContainSingle(Role.Viewer);
    }

    [Fact]
    public async Task RegisterAsync_WithExplicitOperatorRole_ShouldCreateUserWithOperatorRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var request = new RegisterRequest("operator_test", "operator@example.com", "OperatorPassword123!", "Operator");

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.User.Roles.Should().ContainSingle(Role.Operator);
    }

    [Fact]
    public async Task RegisterAsync_WithExplicitAdminRole_ShouldCreateUserWithAdminRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var request = new RegisterRequest("admin_test", "admin@example.com", "AdminPassword123!", "Admin");

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.User.Roles.Should().ContainSingle(Role.Admin);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnPersistedViewerRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var regResponse = await authService.RegisterAsync(new RegisterRequest("view_me", "viewme@example.com", "Password123!", "Viewer"));

        // Act
        var userDto = await authService.GetCurrentUserAsync(regResponse.User.Id);

        // Assert
        userDto.Should().NotBeNull();
        userDto.Roles.Should().ContainSingle(Role.Viewer);
    }

    [Fact]
    public async Task LoginAsync_JwtToken_ShouldContainPersistedRoleClaim()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        await authService.RegisterAsync(new RegisterRequest("jwt_viewer", "jwt_viewer@example.com", "Password123!", "Viewer"));

        // Act
        var loginResponse = await authService.LoginAsync(new LoginRequest("jwt_viewer", "Password123!"));

        // Assert
        loginResponse.Token.Should().NotBeNullOrEmpty();
        
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(loginResponse.Token);
        var roleClaims = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        roleClaims.Should().ContainSingle(Role.Viewer);
    }

    [Fact]
    public async Task RegisterAsync_FirstUserWithoutRole_ShouldReceiveAdminRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var request = new RegisterRequest("firstuser", "first@example.com", "Password123!");

        // Act
        var response = await authService.RegisterAsync(request);

        // Assert
        response.Should().NotBeNull();
        response.User.Roles.Should().ContainSingle(Role.Admin);
    }

    [Fact]
    public async Task RegisterAsync_SubsequentUserWithoutRole_ShouldReceiveViewerRole()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        await authService.RegisterAsync(new RegisterRequest("firstuser", "first@example.com", "Password123!"));

        // Act
        var response = await authService.RegisterAsync(new RegisterRequest("seconduser", "second@example.com", "Password123!"));

        // Assert
        response.Should().NotBeNull();
        response.User.Roles.Should().ContainSingle(Role.Viewer);
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateUsername_ShouldThrowConflictException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var request = new RegisterRequest("existinguser", "first@example.com", "Password123!");
        await authService.RegisterAsync(request);

        var duplicateRequest = new RegisterRequest("existinguser", "second@example.com", "Password123!");

        // Act
        var act = async () => await authService.RegisterAsync(duplicateRequest);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*already registered*");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var regRequest = new RegisterRequest("user1", "user1@example.com", "CorrectPass123!");
        await authService.RegisterAsync(regRequest);

        var loginRequest = new LoginRequest("user1", "WrongPass123!");

        // Act
        var act = async () => await authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*Invalid username or password*");
    }

    [Fact]
    public async Task LoginAsync_WithInactiveUser_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var passwordService = new PasswordService();
        var jwtGenerator = new JwtTokenGenerator(Microsoft.Extensions.Options.Options.Create(new JwtOptions()));
        var authService = new AuthService(db, passwordService, jwtGenerator);

        var regRequest = new RegisterRequest("inactiveuser", "inactive@example.com", "Password123!");
        var regResponse = await authService.RegisterAsync(regRequest);

        var userInDb = await db.Users.FindAsync(regResponse.User.Id);
        userInDb!.Deactivate();
        await db.SaveChangesAsync();

        var loginRequest = new LoginRequest("inactiveuser", "Password123!");

        // Act
        var act = async () => await authService.LoginAsync(loginRequest);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage("*inactive*");
    }
}
