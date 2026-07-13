namespace ScriptFlow.API.Application.DTOs;

public sealed record AuthResponse(
    string Email,
    string Role,
    string Token,
    DateTime ExpiresAtUtc);
