using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Domain.Entities;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Mappings;

public static class MappingExtensions
{
    public static PatientDto ToDto(this Patient patient) =>
        new(patient.Id, patient.FirstName, patient.LastName, patient.Address, patient.Nhi.Value,
            patient.DateOfBirth, patient.Gender, patient.PhoneNumber, patient.Email);

    public static ProviderDto ToDto(this Provider provider) =>
        new(provider.Id, provider.FirstName, provider.LastName, provider.Type, provider.NzmcNo, provider.PracticeLocationId,
            provider.Email, provider.PhoneNumber, provider.Qualification);

    public static MedicineDto ToDto(this Medicine medicine) =>
        new(medicine.Id, medicine.Name, medicine.Sctid, medicine.Form);

    public static PracticeDto ToDto(this Practice practice) =>
        new(practice.Id, practice.Name);

    public static PracticeLocationDto ToDto(this PracticeLocation location) =>
        new(location.Id, location.PracticeId, location.Name, location.HpiNumber.Value, location.Address, location.Phone);

    public static MedicationDto ToDto(this PrescriptionMedication medication, IReadOnlyDictionary<Guid, Medicine> medicinesById)
    {
        var medicineName = medicinesById.TryGetValue(medication.MedicineId, out var medicine) ? medicine.Name : "Unknown";
        return new MedicationDto(
            medication.Id, medication.MedicineId, medicineName, medication.TakeValue,
            medication.Frequency, medication.Duration, medication.Quantity, medication.Directions,
            medication.Route, medication.Strength, medication.IsPrn, medication.Notes,
            medication.Repeats, medication.RepeatsUsed);
    }

    public static PrescriptionDto ToDto(
        this Prescription prescription,
        IReadOnlyDictionary<Guid, Medicine> medicinesById,
        IReadOnlyDictionary<Guid, Patient>? patientsById = null,
        IReadOnlyDictionary<Guid, Provider>? providersById = null) =>
        new(
            prescription.Id,
            prescription.Scid.Value,
            prescription.PatientId,
            prescription.ProviderId,
            prescription.PracticeLocationId,
            prescription.Status,
            prescription.RepeatOfPrescriptionId,
            prescription.CreatedAtUtc,
            prescription.SignedAtUtc,
            prescription.RejectionReason,
            prescription.Medications.Select(m => m.ToDto(medicinesById)).ToList(),
            patientsById?.GetValueOrDefault(prescription.PatientId)?.FullName,
            providersById?.GetValueOrDefault(prescription.ProviderId)?.FullName,
            prescription.Status == PrescriptionStatus.Acknowledged && prescription.Medications.Any(m => m.HasRepeatsRemaining));
}
