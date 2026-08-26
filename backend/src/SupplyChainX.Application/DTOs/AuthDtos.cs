namespace SupplyChainX.Application.DTOs;

public record RegisterRequest(
    string Username,
    string Email,
    string Password,
    string? Role = null
);

public record LoginRequest(
    string Username,
    string Password
);

public record UserDto(
    Guid Id,
    string Username,
    string Email,
    IReadOnlyList<string> Roles,
    bool IsActive,
    DateTime CreatedAtUtc
);

public record AuthResponse(
    string Token,
    UserDto User
);
