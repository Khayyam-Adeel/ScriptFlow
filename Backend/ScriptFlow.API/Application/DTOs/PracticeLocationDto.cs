namespace ScriptFlow.API.Application.DTOs;

public sealed record PracticeLocationDto(
    Guid Id,
    Guid PracticeId,
    string Name,
    string HpiNumber);
