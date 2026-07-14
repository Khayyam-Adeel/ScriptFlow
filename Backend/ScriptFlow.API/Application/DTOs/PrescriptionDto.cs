using Shared.contract.Enums;

namespace ScriptFlow.API.Application.DTOs;

public sealed record PrescriptionDto(
    Guid Id,
    string Scid,
    Guid PatientId,
    Guid ProviderId,
    Guid PracticeLocationId,
    PrescriptionStatus Status,
    Guid? RepeatOfPrescriptionId,
    DateTime CreatedAtUtc,
    DateTime? SignedAtUtc,
    string? RejectionReason,
    IReadOnlyCollection<MedicationDto> Medications);
