using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record RedispatchPrescriptionCommand(Guid PrescriptionId) : IRequest<PrescriptionDto>;
