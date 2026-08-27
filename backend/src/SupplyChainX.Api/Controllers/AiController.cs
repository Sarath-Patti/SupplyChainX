using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;

namespace SupplyChainX.Api.Controllers;

[ApiController]
[Route("ai")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiCopilotService _aiCopilotService;

    public AiController(IAiCopilotService aiCopilotService)
    {
        _aiCopilotService = aiCopilotService;
    }

    [HttpPost("chat")]
    public async Task<ActionResult<ChatResponse>> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Chat request message cannot be empty." });
        }

        var response = await _aiCopilotService.ChatAsync(request, User, cancellationToken);
        return Ok(response);
    }
}
