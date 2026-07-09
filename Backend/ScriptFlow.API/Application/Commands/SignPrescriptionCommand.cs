using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record SignPrescriptionCommand(Guid PrescriptionId) : IRequest<PrescriptionDto>;
