namespace SupplyChainX.Application.DTOs;

public record ChatMessageDto(
    string Role,
    string Content
);

public record ChatRequest(
    string Message,
    List<ChatMessageDto>? History = null
);

public record ChatResponse(
    string Response,
    List<string>? ToolsInvoked = null,
    DateTime TimestampUtc = default
);
