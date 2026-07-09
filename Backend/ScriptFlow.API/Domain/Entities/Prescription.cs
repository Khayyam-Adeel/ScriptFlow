using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.contract.Enums;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>
/// Aggregate root for a prescription. Owns the medication list and the lifecycle state
/// machine: Created -&gt; Signed -&gt; Dispatched -&gt; Acknowledged/Rejected, with Expired
/// reachable from any non-terminal state. This API only drives Created -&gt; Signed and
/// spawns repeats; the later states are transitioned by Dispatch.Worker in a future pass.
/// </summary>
public sealed class Prescription
{
    private readonly List<PrescriptionMedication> _medications = new();

    public Guid Id { get; }
    public Scid Scid { get; }
    public Guid PatientId { get; }
    public Guid ProviderId { get; }
    public Guid PracticeLocationId { get; }
    public Guid? RepeatOfPrescriptionId { get; }
    public PrescriptionStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; }
    public DateTime? SignedAtUtc { get; private set; }

    public IReadOnlyCollection<PrescriptionMedication> Medications => _medications.AsReadOnly();

    public Prescription(
        Guid id,
        Scid scid,
        Guid patientId,
        Guid providerId,
        Guid practiceLocationId,
        IEnumerable<PrescriptionMedication> medications,
        Guid? repeatOfPrescriptionId = null)
    {
        var medicationList = medications?.ToList() ?? new List<PrescriptionMedication>();
        if (medicationList.Count == 0)
        {
            throw new DomainException("A prescription must have at least one medication.");
        }

        Id = id;
        Scid = scid ?? throw new ArgumentNullException(nameof(scid));
        PatientId = patientId;
        ProviderId = providerId;
        PracticeLocationId = practiceLocationId;
        RepeatOfPrescriptionId = repeatOfPrescriptionId;
        Status = PrescriptionStatus.Created;
        CreatedAtUtc = DateTime.UtcNow;
        _medications.AddRange(medicationList);
    }

    public void UpdateMedications(IEnumerable<PrescriptionMedication> medications)
    {
        EnsureStatus(PrescriptionStatus.Created, "update");

        var medicationList = medications?.ToList() ?? new List<PrescriptionMedication>();
        if (medicationList.Count == 0)
        {
            throw new DomainException("A prescription must have at least one medication.");
        }

        _medications.Clear();
        _medications.AddRange(medicationList);
    }

    public void Sign()
    {
        EnsureStatus(PrescriptionStatus.Created, "sign");

        Status = PrescriptionStatus.Signed;
        SignedAtUtc = DateTime.UtcNow;
    }

    public Prescription Repeat(Guid newId, Scid newScid)
    {
        if (Status is not (PrescriptionStatus.Signed or PrescriptionStatus.Dispatched or PrescriptionStatus.Acknowledged))
        {
            throw new InvalidPrescriptionStateException(
                $"Cannot repeat a prescription in {Status} status; it must be Signed, Dispatched, or Acknowledged.");
        }

        var repeatedMedications = _medications.Select(m => new PrescriptionMedication(
            Guid.NewGuid(), m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions));

        return new Prescription(newId, newScid, PatientId, ProviderId, PracticeLocationId, repeatedMedications, repeatOfPrescriptionId: Id);
    }

    private void EnsureStatus(PrescriptionStatus required, string action)
    {
        if (Status != required)
        {
            throw new InvalidPrescriptionStateException(
                $"Cannot {action} a prescription in {Status} status; it must be {required}.");
        }
    }
}
