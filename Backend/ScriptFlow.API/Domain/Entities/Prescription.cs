using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.contract.Enums;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>
/// Aggregate root for a prescription. Owns the medication list and the lifecycle state
/// machine: Created -&gt; Signed -&gt; Dispatched -&gt; Acknowledged/Rejected, with Expired
/// reachable from any non-terminal state (Created, Signed, or Dispatched). This API drives
/// Created -&gt; Signed directly (SignPrescriptionCommandHandler) and Acknowledged -&gt; Signed
/// again for a repeat dispense (RequestRepeatDispenseCommandHandler); Dispatched
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

    /// <summary>Set only by Reject() - the pharmacy's reason for a business rejection (e.g.
    /// "OutOfStock"), surfaced to the prescriber so they know what to do next (see
    /// PrescriptionRejectedEvent, the only source of this value).</summary>
    public string? RejectionReason { get; private set; }

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
            PrescriptionStatus.Created, DateTime.UtcNow, signedAtUtc: null, rejectionReason: null,
            RequireNonEmpty(medications))
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
        string? rejectionReason,
        IEnumerable<PrescriptionMedication> medications)
        => new(
            id, scid, patientId, providerId, practiceLocationId, repeatOfPrescriptionId,
            status, createdAtUtc, signedAtUtc, rejectionReason, medications.ToList());

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
        string? rejectionReason,
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
        RejectionReason = rejectionReason;
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

    public void Acknowledge(bool isRepeatDispense = false)
    {
        EnsureStatus(PrescriptionStatus.Dispatched, "acknowledge");

        Status = PrescriptionStatus.Acknowledged;

        if (isRepeatDispense)
        {
            var updated = _medications
                .Select(m => m.HasRepeatsRemaining ? m.WithRepeatRecorded() : m)
                .ToList();
            _medications.Clear();
            _medications.AddRange(updated);
        }
    }

    /// <summary>
    /// A first-time rejection (isRepeatDispense = false) is terminal, matching the pipeline's
    /// existing behavior. A rejection of a repeat-dispense attempt does not consume a repeat and
    /// is not terminal - the pharmacy simply couldn't fill it this time, so the prescription
    /// reverts to Acknowledged and can be retried later.
    /// </summary>
    public void Reject(string reason, bool isRepeatDispense = false)
    {
        EnsureStatus(PrescriptionStatus.Dispatched, "reject");

        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new DomainException("A rejection reason is required.");
        }

        if (isRepeatDispense)
        {
            Status = PrescriptionStatus.Acknowledged;
            return;
        }

        Status = PrescriptionStatus.Rejected;
        RejectionReason = reason;
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

    /// <summary>
    /// Re-enters the same prescription into the dispatch pipeline for a repeat dispense - no new
    /// signature is required, since the original signature already authorizes each medication's
    /// Repeats count. The whole script is dispensed together as long as at least one medication
    /// still has a repeat remaining; medications that have exhausted their repeats are carried
    /// over unchanged (see Acknowledge).
    /// </summary>
    public void RequestRepeatDispense()
    {
        EnsureStatus(PrescriptionStatus.Acknowledged, "request a repeat dispense for");

        if (!_medications.Any(m => m.HasRepeatsRemaining))
        {
            throw new InvalidPrescriptionStateException(
                "Cannot request a repeat dispense: no medication has any repeats remaining.");
        }

        Status = PrescriptionStatus.Signed;
    }

    /// <summary>
    /// Manual recovery for a prescription stuck at Dispatched - e.g. Dispatch.Worker's pharmacy
    /// call exhausted its retries and the delivery never actually completed (see
    /// PrescriptionSignedEventHandler). Rewinds to Signed rather than staying at Dispatched so
    /// that when Dispatch.Worker attempts delivery again, PrescriptionDispatchedEventHandler's
    /// own Dispatch() call (which requires Signed) succeeds cleanly instead of finding the
    /// aggregate already Dispatched and dead-lettering a harmless-but-noisy duplicate.
    /// </summary>
    public void RequestRedispatch()
    {
        EnsureStatus(PrescriptionStatus.Dispatched, "redispatch");

        Status = PrescriptionStatus.Signed;
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
