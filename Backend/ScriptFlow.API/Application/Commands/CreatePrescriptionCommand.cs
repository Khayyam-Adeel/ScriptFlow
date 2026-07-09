using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Commands;

public sealed record CreatePrescriptionCommand(
    Guid PatientId,
    Guid ProviderId,
    Guid PracticeLocationId,
    IReadOnlyCollection<MedicationLine> Medications) : IRequest<PrescriptionDto>;
