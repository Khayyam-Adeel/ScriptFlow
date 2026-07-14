using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record CreatePracticeLocationCommand(
    Guid PracticeId,
    string Name,
    string HpiNo,
    string HpiExtension,
    string Address,
    string Phone) : IRequest<PracticeLocationDto>;
