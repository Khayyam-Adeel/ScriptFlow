using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.contract.Enums;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>
/// Aggregate root for a prescription. Owns the medication list and the lifecycle state
/// machine: Created -&gt; Signed -&gt; Dispatched -&gt; Acknowledged/Rejected, with Expired
/// reachable from any non-terminal state (Created, Signed, or Dispatched). This API drives
/// Created -&gt; Signed directly (SignPrescriptionCommandHandler) and spawns repeats; Dispatched
/// and Acknowledged/Rejected are driven by PrescriptionDispatchedEventHandler /
/// PrescriptionAcknowledgedEventHandler / PrescriptionRejectedEventHandler reacting to events
/// from Dispatch.Worker, and Expired is driven by PrescriptionExpiryService's periodic sweep.
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
        : this(
            id, scid, patientId, providerId, practiceLocationId, repeatOfPrescriptionId,
            PrescriptionStatus.Created, DateTime.UtcNow, signedAtUtc: null, RequireNonEmpty(medications))
    {
    }

    /// <summary>
    /// Rehydrates a Prescription from persisted state (real Status/CreatedAtUtc/SignedAtUtc),
    /// bypassing the "new prescription" defaults the public constructor applies. Only
    /// SQL-backed repositories (same assembly) reading rows back from the database should call
    /// this - Application-layer code creating a genuinely new prescription must use the public
    /// constructor instead.
    /// </summary>
    internal static Prescription Rehydrate(
        Guid id,
        Scid scid,
        Guid patientId,
        Guid providerId,
        Guid practiceLocationId,
        Guid? repeatOfPrescriptionId,
        PrescriptionStatus status,
        DateTime createdAtUtc,
        DateTime? signedAtUtc,
        IEnumerable<PrescriptionMedication> medications)
        => new(
            id, scid, patientId, providerId, practiceLocationId, repeatOfPrescriptionId,
            status, createdAtUtc, signedAtUtc, medications.ToList());

    private Prescription(
        Guid id,
        Scid scid,
        Guid patientId,
        Guid providerId,
        Guid practiceLocationId,
        Guid? repeatOfPrescriptionId,
        PrescriptionStatus status,
        DateTime createdAtUtc,
        DateTime? signedAtUtc,
        List<PrescriptionMedication> medications)
    {
        Id = id;
        Scid = scid ?? throw new ArgumentNullException(nameof(scid));
        PatientId = patientId;
        ProviderId = providerId;
        PracticeLocationId = practiceLocationId;
        RepeatOfPrescriptionId = repeatOfPrescriptionId;
        Status = status;
        CreatedAtUtc = createdAtUtc;
        SignedAtUtc = signedAtUtc;
        _medications.AddRange(medications);
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

    public void Dispatch()
    {
        EnsureStatus(PrescriptionStatus.Signed, "dispatch");

        Status = PrescriptionStatus.Dispatched;
    }

    public void Acknowledge()
    {
        EnsureStatus(PrescriptionStatus.Dispatched, "acknowledge");

        Status = PrescriptionStatus.Acknowledged;
    }

    public void Reject()
    {
        EnsureStatus(PrescriptionStatus.Dispatched, "reject");

        Status = PrescriptionStatus.Rejected;
    }

    /// <summary>Reachable from any non-terminal state - a prescription can go stale while still
    /// Created (never signed), Signed (never dispatched), or Dispatched (pharmacy never
    /// answered) - see PrescriptionExpiryService for what actually calls this and when.</summary>
    public void Expire()
    {
        if (Status is PrescriptionStatus.Acknowledged or PrescriptionStatus.Rejected or PrescriptionStatus.Expired)
        {
            throw new InvalidPrescriptionStateException(
                $"Cannot expire a prescription in {Status} status; it is already terminal.");
        }

        Status = PrescriptionStatus.Expired;
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

    private static List<PrescriptionMedication> RequireNonEmpty(IEnumerable<PrescriptionMedication> medications)
    {
        var medicationList = medications?.ToList() ?? new List<PrescriptionMedication>();
        if (medicationList.Count == 0)
        {
            throw new DomainException("A prescription must have at least one medication.");
        }

        return medicationList;
    }
}
