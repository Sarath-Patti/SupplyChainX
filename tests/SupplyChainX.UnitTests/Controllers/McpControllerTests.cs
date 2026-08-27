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

public class McpControllerTests
{
    private readonly IMcpServerService _mcpServerServiceMock;
    private readonly McpController _controller;

    public McpControllerTests()
    {
        _mcpServerServiceMock = Substitute.For<IMcpServerService>();
        _controller = new McpController(_mcpServerServiceMock);

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
    public void ListTools_ShouldReturnOkWithMcpTools()
    {
        // Arrange
        var expectedTools = new McpToolsListResponse(new List<McpToolDefinition>
        {
            new("supplychainx_get_products", "Retrieves active products", new { })
        });
        _mcpServerServiceMock.ListTools().Returns(expectedTools);

        // Act
        var result = _controller.ListTools();

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<McpToolsListResponse>().Subject;
        response.Tools.Should().HaveCount(1);
        response.Tools[0].Name.Should().Be("supplychainx_get_products");
    }

    [Fact]
    public async Task CallTool_WithEmptyToolName_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.CallTool(new McpToolCallRequest(""), CancellationToken.None);

        // Assert
        result.Result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task CallTool_WithValidRequest_ShouldReturnOkWithMcpToolResponse()
    {
        // Arrange
        var request = new McpToolCallRequest("supplychainx_get_products");
        var expectedResponse = new McpToolCallResponse(new List<McpContentBlock>
        {
            new("text", "[{\"Id\":\"prod-1\",\"Name\":\"Item A\"}]")
        });

        _mcpServerServiceMock
            .CallToolAsync(Arg.Any<McpToolCallRequest>(), Arg.Any<ClaimsPrincipal>(), Arg.Any<CancellationToken>())
            .Returns(expectedResponse);

        // Act
        var result = await _controller.CallTool(request, CancellationToken.None);

        // Assert
        var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeOfType<McpToolCallResponse>().Subject;
        response.Content.Should().HaveCount(1);
        response.Content[0].Text.Should().Contain("Item A");
    }
}
