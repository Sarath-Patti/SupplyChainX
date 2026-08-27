namespace SupplyChainX.Application.DTOs;

public record McpToolDefinition(
    string Name,
    string Description,
    object InputSchema
);

public record McpToolsListResponse(
    List<McpToolDefinition> Tools
);

public record McpToolCallRequest(
    string Name,
    Dictionary<string, object>? Arguments = null
);

public record McpContentBlock(
    string Type,
    string Text
);

public record McpToolCallResponse(
    List<McpContentBlock> Content,
    bool IsError = false
);
