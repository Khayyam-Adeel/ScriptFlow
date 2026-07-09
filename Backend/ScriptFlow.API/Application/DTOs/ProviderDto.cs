using Shared.contract.Enums;

namespace ScriptFlow.API.Application.DTOs;

public sealed record ProviderDto(
    Guid Id,
    string FirstName,
    string LastName,
    ProviderType Type,
    string NzmcNo,
    Guid PracticeLocationId);
