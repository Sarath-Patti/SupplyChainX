namespace SupplyChainX.Application.DTOs;

public record ChatMessageDto(
    string Role,
    string Content
);

public record ChatRequest(
    string Message,
    List<ChatMessageDto>? History = null
);

public record AgentActivityStep(
    string Step,
    string ToolName,
    string Status,
    string Details
);

public record ChatResponse(
    string Response,
    List<string>? ToolsInvoked = null,
    List<AgentActivityStep>? ActivityTrace = null,
    DateTime TimestampUtc = default
);
