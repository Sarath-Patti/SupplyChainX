using System.Security.Claims;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Infrastructure.Services.Ai.Plugins;

namespace SupplyChainX.Infrastructure.Services.Mcp;

public class McpServerService : IMcpServerService
{
    private readonly SupplyChainPlugin _supplyChainPlugin;
    private readonly ILogger<McpServerService> _logger;

    public McpServerService(
        SupplyChainPlugin supplyChainPlugin,
        ILogger<McpServerService> logger)
    {
        _supplyChainPlugin = supplyChainPlugin;
        _logger = logger;
    }

    public McpToolsListResponse ListTools()
    {
        var tools = new List<McpToolDefinition>
        {
            new(
                Name: "supplychainx_get_products",
                Description: "Retrieves active products from the SupplyChainX catalog with pagination.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        page = new { type = "integer", description = "Page number (default 1)" },
                        pageSize = new { type = "integer", description = "Page size (default 50)" }
                    }
                }
            ),
            new(
                Name: "supplychainx_get_warehouses",
                Description: "Retrieves active warehouse facilities in the SupplyChainX network.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        page = new { type = "integer", description = "Page number (default 1)" },
                        pageSize = new { type = "integer", description = "Page size (default 50)" }
                    }
                }
            ),
            new(
                Name: "supplychainx_get_inventory",
                Description: "Retrieves stock records across warehouses including available and reserved stock.",
                InputSchema: new
                {
                    type = "object",
                    properties = new
                    {
                        page = new { type = "integer", description = "Page number (default 1)" },
                        pageSize = new { type = "integer", description = "Page size (default 50)" }
                    }
                }
            ),
            new(
                Name: "supplychainx_get_low_stock",
                Description: "Retrieves inventory items that are currently low in stock (available stock at or below minimum threshold).",
                InputSchema: new
                {
                    type = "object",
                    properties = new { }
                }
            )
        };

        return new McpToolsListResponse(tools);
    }

    public async Task<McpToolCallResponse> CallToolAsync(
        McpToolCallRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication is required to invoke SupplyChainX MCP tools.");
        }

        var username = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity.Name ?? "Authenticated User";
        _logger.LogInformation("MCP Tool Invocation: '{ToolName}' requested by user {Username}", request.Name, username);

        try
        {
            switch (request.Name.ToLowerInvariant())
            {
                case "supplychainx_get_products":
                {
                    var page = GetIntArg(request.Arguments, "page", 1);
                    var pageSize = GetIntArg(request.Arguments, "pageSize", 50);
                    var products = await _supplyChainPlugin.GetProductsAsync(page, pageSize, cancellationToken);
                    var json = JsonSerializer.Serialize(products, new JsonSerializerOptions { WriteIndented = true });
                    return new McpToolCallResponse(new List<McpContentBlock> { new("text", json) });
                }

                case "supplychainx_get_warehouses":
                {
                    var page = GetIntArg(request.Arguments, "page", 1);
                    var pageSize = GetIntArg(request.Arguments, "pageSize", 50);
                    var warehouses = await _supplyChainPlugin.GetWarehousesAsync(page, pageSize, cancellationToken);
                    var json = JsonSerializer.Serialize(warehouses, new JsonSerializerOptions { WriteIndented = true });
                    return new McpToolCallResponse(new List<McpContentBlock> { new("text", json) });
                }

                case "supplychainx_get_inventory":
                {
                    var page = GetIntArg(request.Arguments, "page", 1);
                    var pageSize = GetIntArg(request.Arguments, "pageSize", 50);
                    var inventory = await _supplyChainPlugin.GetInventoryAsync(page, pageSize, cancellationToken);
                    var json = JsonSerializer.Serialize(inventory, new JsonSerializerOptions { WriteIndented = true });
                    return new McpToolCallResponse(new List<McpContentBlock> { new("text", json) });
                }

                case "supplychainx_get_low_stock":
                {
                    var lowStock = await _supplyChainPlugin.GetLowStockItemsAsync(cancellationToken);
                    var json = JsonSerializer.Serialize(lowStock, new JsonSerializerOptions { WriteIndented = true });
                    return new McpToolCallResponse(new List<McpContentBlock> { new("text", json) });
                }

                default:
                    return new McpToolCallResponse(
                        new List<McpContentBlock> { new("text", $"Unknown MCP tool '{request.Name}'.") },
                        IsError: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing MCP tool '{ToolName}'", request.Name);
            return new McpToolCallResponse(
                new List<McpContentBlock> { new("text", $"MCP tool execution failed: {ex.Message}") },
                IsError: true);
        }
    }

    private static int GetIntArg(Dictionary<string, object>? args, string key, int defaultValue)
    {
        if (args == null || !args.TryGetValue(key, out var val)) return defaultValue;
        if (val is JsonElement elem && elem.ValueKind == JsonValueKind.Number) return elem.GetInt32();
        if (int.TryParse(val.ToString(), out var result)) return result;
        return defaultValue;
    }
}
