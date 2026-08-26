using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SupplyChainX.Api.Controllers;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Application.Services;
using SupplyChainX.Domain.Entities;
using Xunit;

namespace SupplyChainX.UnitTests.Controllers;

public class AuthControllerTests
{
    private readonly IAuthService _authService = Substitute.For<IAuthService>();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(_authService);
    }

    [Fact]
    public async Task Register_WithExplicitViewerRole_ShouldReturnAuthResponseWithViewerRole()
    {
        // Arrange
        var request = new RegisterRequest("viewer_test", "viewer@example.com", "Password123!", "Viewer");
        var expectedResponse = new AuthResponse(
            "dummy_token",
            new UserDto(Guid.NewGuid(), "viewer_test", "viewer@example.com", new[] { Role.Viewer }, true, DateTime.UtcNow)
        );

        _authService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.User.Roles.Should().ContainSingle(Role.Viewer);
    }

    [Fact]
    public async Task Register_WithExplicitOperatorRole_ShouldReturnAuthResponseWithOperatorRole()
    {
        // Arrange
        var request = new RegisterRequest("op_test", "op@example.com", "Password123!", "Operator");
        var expectedResponse = new AuthResponse(
            "dummy_token",
            new UserDto(Guid.NewGuid(), "op_test", "op@example.com", new[] { Role.Operator }, true, DateTime.UtcNow)
        );

        _authService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.User.Roles.Should().ContainSingle(Role.Operator);
    }

    [Fact]
    public async Task Register_WithExplicitAdminRole_ShouldReturnAuthResponseWithAdminRole()
    {
        // Arrange
        var request = new RegisterRequest("admin_test", "admin@example.com", "Password123!", "Admin");
        var expectedResponse = new AuthResponse(
            "dummy_token",
            new UserDto(Guid.NewGuid(), "admin_test", "admin@example.com", new[] { Role.Admin }, true, DateTime.UtcNow)
        );

        _authService.RegisterAsync(request, Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.Register(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var authResponse = okResult.Value.Should().BeOfType<AuthResponse>().Subject;
        authResponse.User.Roles.Should().ContainSingle(Role.Admin);
    }

    [Fact]
    public async Task GetCurrentUser_WhenAuthenticated_ShouldReturnUserDtoWithRoles()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userDto = new UserDto(userId, "auth_user", "auth@example.com", new[] { Role.Viewer }, true, DateTime.UtcNow);

        _authService.GetCurrentUserAsync(userId, Arg.Any<CancellationToken>())
            .Returns(userDto);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, "auth_user")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var claimsPrincipal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal }
        };

        // Act
        var result = await _controller.GetCurrentUser(CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedUser = okResult.Value.Should().BeOfType<UserDto>().Subject;
        returnedUser.Roles.Should().ContainSingle(Role.Viewer);
    }
}
