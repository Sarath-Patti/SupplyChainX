using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;

namespace SupplyChainX.Api.Controllers;

[ApiController]
[Route("mcp")]
[Authorize]
public class McpController : ControllerBase
{
    private readonly IMcpServerService _mcpServerService;

    public McpController(IMcpServerService mcpServerService)
    {
        _mcpServerService = mcpServerService;
    }

    [HttpGet("tools")]
    public ActionResult<McpToolsListResponse> ListTools()
    {
        var response = _mcpServerService.ListTools();
        return Ok(response);
    }

    [HttpPost("tools/call")]
    public async Task<ActionResult<McpToolCallResponse>> CallTool(
        [FromBody] McpToolCallRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { error = "Tool name cannot be empty." });
        }

        var response = await _mcpServerService.CallToolAsync(request, User, cancellationToken);
        return Ok(response);
    }
}
