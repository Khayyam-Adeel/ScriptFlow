using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record UpdatePrescriptionCommand(
    Guid PrescriptionId,
    IReadOnlyCollection<MedicationLine> Medications) : IRequest<PrescriptionDto>;
