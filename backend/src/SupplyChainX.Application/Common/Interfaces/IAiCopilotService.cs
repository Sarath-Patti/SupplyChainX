using System.Security.Claims;
using SupplyChainX.Application.DTOs;

namespace SupplyChainX.Application.Common.Interfaces;

public interface IAiCopilotService
{
    Task<ChatResponse> ChatAsync(
        ChatRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
