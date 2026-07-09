namespace ScriptFlow.API.Application.DTOs;

public sealed record AuthResponse(
    string Email,
    string Token,
    DateTime ExpiresAtUtc);
