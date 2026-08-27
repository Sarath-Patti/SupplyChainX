using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using SupplyChainX.Api.Controllers;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using Xunit;

namespace SupplyChainX.UnitTests.Controllers;

public class AiControllerTests
{
    private readonly IAiCopilotService _aiCopilotServiceMock;
    private readonly AiController _controller;

    public AiControllerTests()
    {
        _aiCopilotServiceMock = Substitute.For<IAiCopilotService>();
        _controller = new AiController(_aiCopilotServiceMock);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user-123"),
            new Claim(ClaimTypes.Name, "testuser"),
            new Claim(ClaimTypes.Role, "Viewer")
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal }
        };
    }

    [Fact]
    public async Task Chat_WithNullOrEmptyMessage_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.Chat(new ChatRequest("  "), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Chat_WithValidRequest_ShouldReturnOkWithChatResponse()
    {
        // Arrange
        var request = new ChatRequest("Which products are low in stock?");
        var expectedResponse = new ChatResponse(
            Response: "All stock levels healthy.",
            ToolsInvoked: new List<string> { "GetLowStockItemsAsync" },
            TimestampUtc: DateTime.UtcNow
        );

        _aiCopilotServiceMock
            .ChatAsync(Arg.Any<ChatRequest>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.Chat(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var chatResponse = okResult.Value.Should().BeOfType<ChatResponse>().Subject;
        chatResponse.Response.Should().Be("All stock levels healthy.");
        chatResponse.ToolsInvoked.Should().Contain("GetLowStockItemsAsync");
    }
}
