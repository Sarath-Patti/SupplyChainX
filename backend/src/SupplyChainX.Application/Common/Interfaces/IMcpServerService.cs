using System.Security.Claims;
using SupplyChainX.Application.DTOs;

namespace SupplyChainX.Application.Common.Interfaces;

public interface IMcpServerService
{
    McpToolsListResponse ListTools();
    Task<McpToolCallResponse> CallToolAsync(
        McpToolCallRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
