using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record RequestRepeatDispenseCommand(Guid PrescriptionId) : IRequest<PrescriptionDto>;
