using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using SupplyChainX.Application.Common.Configuration;
using SupplyChainX.Application.Common.Interfaces;
using SupplyChainX.Application.DTOs;
using SupplyChainX.Infrastructure.Services.Ai.Plugins;

namespace SupplyChainX.Infrastructure.Services.Ai;

public class AiCopilotService : IAiCopilotService
{
    private readonly AiOptions _options;
    private readonly SupplyChainPlugin _supplyChainPlugin;
    private readonly ILogger<AiCopilotService> _logger;

    public AiCopilotService(
        IOptions<AiOptions> options,
        SupplyChainPlugin supplyChainPlugin,
        ILogger<AiCopilotService> logger)
    {
        _options = options.Value;
        _supplyChainPlugin = supplyChainPlugin;
        _logger = logger;
    }

    public async Task<ChatResponse> ChatAsync(
        ChatRequest request,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (user.Identity == null || !user.Identity.IsAuthenticated)
        {
            throw new UnauthorizedAccessException("Authentication is required to access the AI Copilot.");
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Chat message cannot be empty.", nameof(request));
        }

        var username = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.Identity.Name ?? "Authenticated User";
        var roles = user.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

        _logger.LogInformation("Processing AI Copilot request for user {Username} with roles [{Roles}]", username, string.Join(", ", roles));

        var toolsInvoked = new List<string>();

        // Check if live OpenAI / Azure OpenAI credentials are configured
        var openAiKey = !string.IsNullOrWhiteSpace(_options.OpenAiApiKey)
            ? _options.OpenAiApiKey
            : Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        var azureKey = !string.IsNullOrWhiteSpace(_options.AzureOpenAiApiKey)
            ? _options.AzureOpenAiApiKey
            : Environment.GetEnvironmentVariable("AZURE_OPENAI_API_KEY");

        if (!string.IsNullOrWhiteSpace(openAiKey) || !string.IsNullOrWhiteSpace(azureKey))
        {
            try
            {
                return await ExecuteSemanticKernelWithLlmAsync(request, openAiKey, azureKey, toolsInvoked, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "External LLM provider invocation failed or unconfigured. Falling back to Grounded Semantic Kernel RAG Orchestrator.");
            }
        }

        // Grounded Semantic Kernel RAG Orchestration Engine (Local Fallback Mode)
        return await ExecuteGroundedRagOrchestratorAsync(request, toolsInvoked, cancellationToken);
    }

    private async Task<ChatResponse> ExecuteSemanticKernelWithLlmAsync(
        ChatRequest request,
        string? openAiKey,
        string? azureKey,
        List<string> toolsInvoked,
        CancellationToken cancellationToken)
    {
        var builder = Kernel.CreateBuilder();
        builder.Plugins.AddFromObject(_supplyChainPlugin, "SupplyChain");

        if (!string.IsNullOrWhiteSpace(azureKey))
        {
            var endpoint = !string.IsNullOrWhiteSpace(_options.AzureOpenAiEndpoint)
                ? _options.AzureOpenAiEndpoint
                : Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? "";
            var deployment = !string.IsNullOrWhiteSpace(_options.AzureOpenAiDeploymentName)
                ? _options.AzureOpenAiDeploymentName
                : "gpt-4o-mini";

            builder.AddAzureOpenAIChatCompletion(deployment, endpoint, azureKey);
        }
        else if (!string.IsNullOrWhiteSpace(openAiKey))
        {
            builder.AddOpenAIChatCompletion(_options.OpenAiModel, openAiKey);
        }

        var kernel = builder.Build();
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage(
            "You are SupplyChainX AI Copilot, an enterprise AI assistant for inventory and supply chain management. " +
            "You MUST ground all answers in real SupplyChainX database data using the provided tools. " +
            "Do NOT fabricate products, warehouses, or quantities. Always state facts based on retrieved inventory context.");

        if (request.History != null)
        {
            foreach (var msg in request.History.TakeLast(6))
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                    history.AddUserMessage(msg.Content);
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                    history.AddAssistantMessage(msg.Content);
            }
        }

        history.AddUserMessage(request.Message);

        var promptExecutionSettings = new PromptExecutionSettings
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        var result = await chatCompletionService.GetChatMessageContentAsync(
            history,
            promptExecutionSettings,
            kernel,
            cancellationToken);

        return new ChatResponse(
            Response: result.Content ?? "I could not generate a response based on the current supply chain data.",
            ToolsInvoked: toolsInvoked,
            TimestampUtc: DateTime.UtcNow
        );
    }

    private async Task<ChatResponse> ExecuteGroundedRagOrchestratorAsync(
        ChatRequest request,
        List<string> toolsInvoked,
        CancellationToken cancellationToken)
    {
        var query = request.Message.ToLowerInvariant();
        var sb = new StringBuilder();

        if ((query.Contains("low") && query.Contains("stock")) || query.Contains("reorder") || query.Contains("threshold") || query.Contains("shortage"))
        {
            toolsInvoked.Add("GetLowStockItemsAsync");
            var lowStock = await _supplyChainPlugin.GetLowStockItemsAsync(cancellationToken);

            sb.AppendLine("### ⚠️ Low Stock & Inventory Alert Summary");
            sb.AppendLine();
            if (lowStock.Count == 0)
            {
                sb.AppendLine("According to current inventory data, all items have healthy stock levels above their minimum stock thresholds.");
            }
            else
            {
                sb.AppendLine($"Found **{lowStock.Count}** inventory items requiring immediate replenishment:");
                sb.AppendLine();
                sb.AppendLine("| Product | Warehouse | Available Stock | Reserved | Minimum Threshold |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var item in lowStock)
                {
                    sb.AppendLine($"| **{item.ProductName}** (`{item.ProductSku}`) | {item.WarehouseName} | `{item.AvailableQuantity}` | `{item.ReservedQuantity}` | `{item.MinimumStockThreshold}` |");
                }
                sb.AppendLine();
                sb.AppendLine("Recommended Action: Reorder stock for the highlighted items above to prevent fulfillment delays.");
            }
        }
        else if (query.Contains("product") || query.Contains("sku") || query.Contains("catalog"))
        {
            if (query.Contains("sku-") || query.Contains("sku"))
            {
                toolsInvoked.Add("GetProductByIdOrSkuAsync");
                toolsInvoked.Add("GetInventoryAsync");

                var products = await _supplyChainPlugin.GetProductsAsync(1, 100, cancellationToken);
                var matchedProduct = products.FirstOrDefault(p => query.Contains(p.Sku.ToLowerInvariant()));

                if (matchedProduct != null)
                {
                    var inventory = await _supplyChainPlugin.GetInventoryAsync(1, 100, cancellationToken);
                    var productInventory = inventory.Where(i => i.ProductId == matchedProduct.Id).ToList();

                    sb.AppendLine($"### 📦 Product & Inventory Breakdown: {matchedProduct.Name}");
                    sb.AppendLine();
                    sb.AppendLine($"- **SKU**: `{matchedProduct.Sku}`");
                    sb.AppendLine($"- **Description**: {matchedProduct.Description ?? "N/A"}");
                    sb.AppendLine($"- **Unit Price**: ${matchedProduct.UnitPrice:F2}");
                    sb.AppendLine($"- **Status**: {(matchedProduct.IsActive ? "Active" : "Inactive")}");
                    sb.AppendLine();
                    sb.AppendLine("#### Current Inventory Distribution:");
                    if (productInventory.Count == 0)
                    {
                        sb.AppendLine("No inventory records found for this product across warehouses.");
                    }
                    else
                    {
                        sb.AppendLine("| Warehouse | Available Stock | Reserved Stock | Minimum Threshold |");
                        sb.AppendLine("|---|---|---|---|");
                        foreach (var inv in productInventory)
                        {
                            sb.AppendLine($"| {inv.WarehouseName} | `{inv.AvailableQuantity}` | `{inv.ReservedQuantity}` | `{inv.MinimumStockThreshold}` |");
                        }
                    }
                }
                else
                {
                    toolsInvoked.Add("GetProductsAsync");
                    sb.AppendLine("### 📦 Product Catalog Overview");
                    sb.AppendLine();
                    sb.AppendLine($"According to current product data, here are the available products in SupplyChainX:");
                    sb.AppendLine();
                    sb.AppendLine("| Name | SKU | Price | Status | Description |");
                    sb.AppendLine("|---|---|---|---|---|");
                    foreach (var p in products)
                    {
                        sb.AppendLine($"| **{p.Name}** | `{p.Sku}` | ${p.UnitPrice:F2} | {(p.IsActive ? "Active" : "Inactive")} | {p.Description ?? "N/A"} |");
                    }
                }
            }
            else
            {
                toolsInvoked.Add("GetProductsAsync");
                var products = await _supplyChainPlugin.GetProductsAsync(1, 100, cancellationToken);

                sb.AppendLine("### 📦 SupplyChainX Product Catalog");
                sb.AppendLine();
                sb.AppendLine($"Retrieved **{products.Count}** active products from the catalog:");
                sb.AppendLine();
                sb.AppendLine("| Name | SKU | Price | Status | Description |");
                sb.AppendLine("|---|---|---|---|---|");
                foreach (var p in products)
                {
                    sb.AppendLine($"| **{p.Name}** | `{p.Sku}` | ${p.UnitPrice:F2} | {(p.IsActive ? "Active" : "Inactive")} | {p.Description ?? "N/A"} |");
                }
            }
        }
        else if (query.Contains("warehouse") || query.Contains("location") || query.Contains("facility"))
        {
            toolsInvoked.Add("GetWarehousesAsync");
            toolsInvoked.Add("GetInventoryAsync");

            var warehouses = await _supplyChainPlugin.GetWarehousesAsync(1, 100, cancellationToken);
            var inventory = await _supplyChainPlugin.GetInventoryAsync(1, 100, cancellationToken);

            sb.AppendLine("### 🏭 Warehouse Network & Capacity Summary");
            sb.AppendLine();
            sb.AppendLine($"Found **{warehouses.Count}** active warehouse facilities:");
            sb.AppendLine();
            sb.AppendLine("| Warehouse Name | Location | Status | Total Stock Available |");
            sb.AppendLine("|---|---|---|---|");

            foreach (var w in warehouses)
            {
                var wInventory = inventory.Where(i => i.WarehouseId == w.Id).ToList();
                var totalAvailable = wInventory.Sum(i => i.AvailableQuantity);
                sb.AppendLine($"| **{w.Name}** | {w.Location} | {(w.IsActive ? "Active" : "Inactive")} | `{totalAvailable}` units |");
            }
        }
        else if (query.Contains("inventory") || query.Contains("stock") || query.Contains("summary"))
        {
            toolsInvoked.Add("GetInventoryAsync");
            var inventory = await _supplyChainPlugin.GetInventoryAsync(1, 100, cancellationToken);

            var totalAvailable = inventory.Sum(i => i.AvailableQuantity);
            var totalReserved = inventory.Sum(i => i.ReservedQuantity);

            sb.AppendLine("### 📋 Executive Inventory Summary");
            sb.AppendLine();
            sb.AppendLine($"According to current inventory telemetry:");
            sb.AppendLine($"- **Total Active Records**: {inventory.Count}");
            sb.AppendLine($"- **Total Available Stock**: `{totalAvailable}` units");
            sb.AppendLine($"- **Total Reserved Stock**: `{totalReserved}` units");
            sb.AppendLine();
            sb.AppendLine("#### Stock Level Breakdown:");
            sb.AppendLine("| Product | Warehouse | Available Stock | Reserved | Minimum Threshold |");
            sb.AppendLine("|---|---|---|---|---|");
            foreach (var i in inventory.Take(15))
            {
                sb.AppendLine($"| **{i.ProductName}** | {i.WarehouseName} | `{i.AvailableQuantity}` | `{i.ReservedQuantity}` | `{i.MinimumStockThreshold}` |");
            }
        }
        else
        {
            toolsInvoked.Add("GetProductsAsync");
            toolsInvoked.Add("GetWarehousesAsync");
            toolsInvoked.Add("GetInventoryAsync");

            var products = await _supplyChainPlugin.GetProductsAsync(1, 10, cancellationToken);
            var warehouses = await _supplyChainPlugin.GetWarehousesAsync(1, 10, cancellationToken);
            var inventory = await _supplyChainPlugin.GetInventoryAsync(1, 10, cancellationToken);

            sb.AppendLine("### 🤖 SupplyChainX AI Assistant");
            sb.AppendLine();
            sb.AppendLine("I am connected to live SupplyChainX data via Semantic Kernel. I can assist you with:");
            sb.AppendLine("- 📦 Product catalog lookups & SKU details");
            sb.AppendLine("- 🏭 Warehouse facility capacities & stock distribution");
            sb.AppendLine("- 📋 Inventory stock levels & reservation tracking");
            sb.AppendLine("- ⚠️ Low-stock alerts and reorder recommendations");
            sb.AppendLine();
            sb.AppendLine($"*Current Telemetry Summary: {products.Count} Products, {warehouses.Count} Warehouses, {inventory.Count} Active Inventory Records.*");
        }

        return new ChatResponse(
            Response: sb.ToString(),
            ToolsInvoked: toolsInvoked,
            TimestampUtc: DateTime.UtcNow
        );
    }
}
