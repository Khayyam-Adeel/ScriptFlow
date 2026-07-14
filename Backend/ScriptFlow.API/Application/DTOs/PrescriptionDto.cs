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
    IReadOnlyCollection<MedicationDto> Medications,
    // Populated only where the handler already has a cheap batch lookup on hand (the
    // prescription list grid) - null elsewhere rather than adding a lookup nothing else needs.
    string? PatientName = null,
    string? ProviderName = null,
    bool CanRepeatDispense = false);
