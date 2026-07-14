using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.contract.Enums;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class PrescriptionTests
{
    private static Scid ValidScid => new("9J0BGVA1B2C");

    private static List<PrescriptionMedication> OneMedication() =>
        new()
        {
            new PrescriptionMedication(
                Guid.NewGuid(), Guid.NewGuid(), "500mg", "Twice daily", "7 days", 14, "Take with food")
        };

    private static Prescription NewPrescription() =>
        new(Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OneMedication());

    private static List<PrescriptionMedication> OneMedicationWithRepeats(int repeats, int repeatsUsed = 0) =>
        new()
        {
            new PrescriptionMedication(
                Guid.NewGuid(), Guid.NewGuid(), "500mg", "Twice daily", "7 days", 14, "Take with food",
                repeats: repeats, repeatsUsed: repeatsUsed)
        };

    private static Prescription AcknowledgedPrescription(int repeats, int repeatsUsed = 0) =>
        Prescription.Rehydrate(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfPrescriptionId: null, PrescriptionStatus.Acknowledged, DateTime.UtcNow, DateTime.UtcNow,
            rejectionReason: null, OneMedicationWithRepeats(repeats, repeatsUsed));

    private static List<PrescriptionMedication> TwoMedicationsMixedRepeats() =>
        new()
        {
            new PrescriptionMedication(
                Guid.NewGuid(), Guid.NewGuid(), "500mg", "Twice daily", "7 days", 14, "Take with food",
                repeats: 2, repeatsUsed: 0),
            new PrescriptionMedication(
                Guid.NewGuid(), Guid.NewGuid(), "10mg", "Once daily", "7 days", 7, "Take in the morning",
                repeats: 0, repeatsUsed: 0)
        };

    private static Prescription AcknowledgedPrescriptionWithMixedRepeats() =>
        Prescription.Rehydrate(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfPrescriptionId: null, PrescriptionStatus.Acknowledged, DateTime.UtcNow, DateTime.UtcNow,
            rejectionReason: null, TwoMedicationsMixedRepeats());

    [Fact]
    public void Constructor_WithValidArguments_StartsInCreatedStatus()
    {
        var id = Guid.NewGuid();
        var patientId = Guid.NewGuid();
        var providerId = Guid.NewGuid();
        var practiceLocationId = Guid.NewGuid();
        var prescription = new Prescription(id, ValidScid, patientId, providerId, practiceLocationId, OneMedication());

        Assert.Equal(id, prescription.Id);
        Assert.Equal(patientId, prescription.PatientId);
        Assert.Equal(providerId, prescription.ProviderId);
        Assert.Equal(practiceLocationId, prescription.PracticeLocationId);
        Assert.Equal(PrescriptionStatus.Created, prescription.Status);
        Assert.Null(prescription.SignedAtUtc);
        Assert.Null(prescription.RepeatOfPrescriptionId);
        Assert.Single(prescription.Medications);
    }

    [Fact]
    public void Constructor_WithNullScid_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Prescription(Guid.NewGuid(), null!, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), OneMedication()));
    }

    [Fact]
    public void Constructor_WithNoMedications_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Prescription(Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), new List<PrescriptionMedication>()));
    }

    [Fact]
    public void Constructor_WithNullMedications_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Prescription(Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null!));
    }

    [Fact]
    public void UpdateMedications_WhileCreated_ReplacesMedicationList()
    {
        var prescription = NewPrescription();
        var replacement = OneMedication();

        prescription.UpdateMedications(replacement);

        Assert.Equal(replacement, prescription.Medications);
    }

    [Fact]
    public void UpdateMedications_WithEmptyList_ThrowsDomainException()
    {
        var prescription = NewPrescription();

        Assert.Throws<DomainException>(() => prescription.UpdateMedications(new List<PrescriptionMedication>()));
    }

    [Fact]
    public void UpdateMedications_AfterSigned_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();
        prescription.Sign();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.UpdateMedications(OneMedication()));
    }

    [Fact]
    public void Sign_WhileCreated_TransitionsToSignedAndSetsSignedAtUtc()
    {
        var prescription = NewPrescription();

        prescription.Sign();

        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
        Assert.NotNull(prescription.SignedAtUtc);
    }

    [Fact]
    public void Sign_WhenAlreadySigned_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();
        prescription.Sign();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Sign());
    }

    [Fact]
    public void Dispatch_WhileSigned_TransitionsToDispatched()
    {
        var prescription = NewPrescription();
        prescription.Sign();

        prescription.Dispatch();

        Assert.Equal(PrescriptionStatus.Dispatched, prescription.Status);
    }

    [Fact]
    public void Dispatch_WhileCreated_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Dispatch());
    }

    [Fact]
    public void Dispatch_WhenAlreadyDispatched_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();
        prescription.Sign();
        prescription.Dispatch();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Dispatch());
    }

    [Fact]
    public void Acknowledge_WhileDispatched_TransitionsToAcknowledged()
    {
        var prescription = NewPrescription();
        prescription.Sign();
        prescription.Dispatch();

        prescription.Acknowledge();

        Assert.Equal(PrescriptionStatus.Acknowledged, prescription.Status);
    }

    [Fact]
    public void Acknowledge_AsFirstDispense_DoesNotRecordARepeat()
    {
        var prescription = new Prescription(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            OneMedicationWithRepeats(repeats: 3));
        prescription.Sign();
        prescription.Dispatch();

        prescription.Acknowledge(isRepeatDispense: false);

        Assert.Equal(0, prescription.Medications.Single().RepeatsUsed);
    }

    [Fact]
    public void Acknowledge_AsRepeatDispense_IncrementsRepeatsUsed()
    {
        var prescription = new Prescription(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            OneMedicationWithRepeats(repeats: 3));
        prescription.Sign();
        prescription.Dispatch();

        prescription.Acknowledge(isRepeatDispense: true);

        Assert.Equal(1, prescription.Medications.Single().RepeatsUsed);
    }

    [Fact]
    public void Acknowledge_WhileCreated_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Acknowledge());
    }

    [Fact]
    public void Acknowledge_WhileSigned_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();
        prescription.Sign();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Acknowledge());
    }

    [Fact]
    public void Reject_WhileDispatched_TransitionsToRejectedAndSetsReason()
    {
        var prescription = NewPrescription();
        prescription.Sign();
        prescription.Dispatch();

        prescription.Reject("OutOfStock");

        Assert.Equal(PrescriptionStatus.Rejected, prescription.Status);
        Assert.Equal("OutOfStock", prescription.RejectionReason);
    }

    [Fact]
    public void Reject_AsRepeatDispense_RevertsToAcknowledgedInsteadOfTerminal()
    {
        var prescription = new Prescription(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            OneMedicationWithRepeats(repeats: 3));
        prescription.Sign();
        prescription.Dispatch();

        prescription.Reject("OutOfStock", isRepeatDispense: true);

        Assert.Equal(PrescriptionStatus.Acknowledged, prescription.Status);
        Assert.Null(prescription.RejectionReason);
        Assert.Equal(0, prescription.Medications.Single().RepeatsUsed);
    }

    [Fact]
    public void Reject_WhileCreated_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Reject("OutOfStock"));
    }

    [Fact]
    public void Reject_WhileSigned_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();
        prescription.Sign();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Reject("OutOfStock"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithNoReason_ThrowsDomainException(string? reason)
    {
        var prescription = NewPrescription();
        prescription.Sign();
        prescription.Dispatch();

        Assert.Throws<DomainException>(() => prescription.Reject(reason!));
    }

    [Theory]
    [InlineData(PrescriptionStatus.Created)]
    [InlineData(PrescriptionStatus.Signed)]
    [InlineData(PrescriptionStatus.Dispatched)]
    public void Expire_FromNonTerminalStatus_TransitionsToExpired(PrescriptionStatus status)
    {
        var prescription = Prescription.Rehydrate(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfPrescriptionId: null, status, DateTime.UtcNow.AddDays(-3), signedAtUtc: null,
            rejectionReason: null, OneMedication());

        prescription.Expire();

        Assert.Equal(PrescriptionStatus.Expired, prescription.Status);
    }

    [Theory]
    [InlineData(PrescriptionStatus.Acknowledged)]
    [InlineData(PrescriptionStatus.Rejected)]
    [InlineData(PrescriptionStatus.Expired)]
    public void Expire_FromTerminalStatus_ThrowsInvalidPrescriptionStateException(PrescriptionStatus status)
    {
        var prescription = Prescription.Rehydrate(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfPrescriptionId: null, status, DateTime.UtcNow.AddDays(-3), signedAtUtc: null,
            rejectionReason: null, OneMedication());

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Expire());
    }

    [Fact]
    public void RequestRepeatDispense_WhileAcknowledgedWithRepeatsRemaining_TransitionsBackToSigned()
    {
        var prescription = AcknowledgedPrescription(repeats: 3, repeatsUsed: 1);

        prescription.RequestRepeatDispense();

        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
    }

    [Theory]
    [InlineData(PrescriptionStatus.Created)]
    [InlineData(PrescriptionStatus.Signed)]
    [InlineData(PrescriptionStatus.Dispatched)]
    [InlineData(PrescriptionStatus.Rejected)]
    [InlineData(PrescriptionStatus.Expired)]
    public void RequestRepeatDispense_FromDisallowedStatus_ThrowsInvalidPrescriptionStateException(PrescriptionStatus status)
    {
        var prescription = Prescription.Rehydrate(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfPrescriptionId: null, status, DateTime.UtcNow, signedAtUtc: null,
            rejectionReason: null, OneMedicationWithRepeats(repeats: 3));

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.RequestRepeatDispense());
    }

    [Fact]
    public void RequestRepeatDispense_WhenNoRepeatsRemaining_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = AcknowledgedPrescription(repeats: 2, repeatsUsed: 2);

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.RequestRepeatDispense());
    }

    [Fact]
    public void RequestRepeatDispense_WhenOnlySomeMedicationsHaveRepeatsRemaining_TransitionsBackToSigned()
    {
        var prescription = AcknowledgedPrescriptionWithMixedRepeats();

        prescription.RequestRepeatDispense();

        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
    }

    [Fact]
    public void Acknowledge_AsRepeatDispenseWithMixedRepeats_OnlyIncrementsMedicationsWithRepeatsRemaining()
    {
        var prescription = new Prescription(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            TwoMedicationsMixedRepeats());
        prescription.Sign();
        prescription.Dispatch();
        prescription.Acknowledge(isRepeatDispense: false);
        prescription.RequestRepeatDispense();
        prescription.Dispatch();

        prescription.Acknowledge(isRepeatDispense: true);

        var medications = prescription.Medications.ToList();
        Assert.Equal(1, medications[0].RepeatsUsed);
        Assert.Equal(0, medications[1].RepeatsUsed);
    }

    [Fact]
    public void Rehydrate_RestoresPersistedState()
    {
        var id = Guid.NewGuid();
        var createdAt = DateTime.UtcNow.AddDays(-1);
        var signedAt = DateTime.UtcNow;
        var repeatOfId = Guid.NewGuid();

        var prescription = Prescription.Rehydrate(
            id, ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfId, PrescriptionStatus.Signed, createdAt, signedAt,
            rejectionReason: null, OneMedication());

        Assert.Equal(id, prescription.Id);
        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
        Assert.Equal(createdAt, prescription.CreatedAtUtc);
        Assert.Equal(signedAt, prescription.SignedAtUtc);
        Assert.Equal(repeatOfId, prescription.RepeatOfPrescriptionId);
        Assert.Null(prescription.RejectionReason);
    }
}
