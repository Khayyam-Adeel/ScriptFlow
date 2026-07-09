using MediatR;
using ScriptFlow.API.Application.DTOs;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Commands;

public sealed record CreateProviderCommand(
    string FirstName,
    string LastName,
    ProviderType Type,
    string NzmcNo,
    Guid PracticeLocationId) : IRequest<ProviderDto>;
