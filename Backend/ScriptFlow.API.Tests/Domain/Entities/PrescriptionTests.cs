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
    public void Acknowledge_WhileSigned_TransitionsToAcknowledged()
    {
        var prescription = NewPrescription();
        prescription.Sign();

        prescription.Acknowledge();

        Assert.Equal(PrescriptionStatus.Acknowledged, prescription.Status);
    }

    [Fact]
    public void Acknowledge_WhileCreated_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Acknowledge());
    }

    [Fact]
    public void Reject_WhileSigned_TransitionsToRejected()
    {
        var prescription = NewPrescription();
        prescription.Sign();

        prescription.Reject();

        Assert.Equal(PrescriptionStatus.Rejected, prescription.Status);
    }

    [Fact]
    public void Reject_WhileCreated_ThrowsInvalidPrescriptionStateException()
    {
        var prescription = NewPrescription();

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Reject());
    }

    [Theory]
    [InlineData(PrescriptionStatus.Signed)]
    [InlineData(PrescriptionStatus.Dispatched)]
    [InlineData(PrescriptionStatus.Acknowledged)]
    public void Repeat_FromAllowedStatus_CreatesNewPrescriptionLinkedToOriginal(PrescriptionStatus status)
    {
        var original = Prescription.Rehydrate(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfPrescriptionId: null, status, DateTime.UtcNow, DateTime.UtcNow, OneMedication());

        var newId = Guid.NewGuid();
        var newScid = new Scid("9AAAAABBBBB");

        var repeat = original.Repeat(newId, newScid);

        Assert.Equal(newId, repeat.Id);
        Assert.Equal(original.Id, repeat.RepeatOfPrescriptionId);
        Assert.Equal(original.PatientId, repeat.PatientId);
        Assert.Equal(original.ProviderId, repeat.ProviderId);
        Assert.Equal(original.PracticeLocationId, repeat.PracticeLocationId);
        Assert.Equal(PrescriptionStatus.Created, repeat.Status);
        Assert.Single(repeat.Medications);
        Assert.NotEqual(original.Medications.Single().Id, repeat.Medications.Single().Id);
    }

    [Theory]
    [InlineData(PrescriptionStatus.Created)]
    [InlineData(PrescriptionStatus.Rejected)]
    public void Repeat_FromDisallowedStatus_ThrowsInvalidPrescriptionStateException(PrescriptionStatus status)
    {
        var prescription = Prescription.Rehydrate(
            Guid.NewGuid(), ValidScid, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
            repeatOfPrescriptionId: null, status, DateTime.UtcNow, signedAtUtc: null, OneMedication());

        Assert.Throws<InvalidPrescriptionStateException>(() => prescription.Repeat(Guid.NewGuid(), new Scid("9AAAAABBBBB")));
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
            repeatOfId, PrescriptionStatus.Signed, createdAt, signedAt, OneMedication());

        Assert.Equal(id, prescription.Id);
        Assert.Equal(PrescriptionStatus.Signed, prescription.Status);
        Assert.Equal(createdAt, prescription.CreatedAtUtc);
        Assert.Equal(signedAt, prescription.SignedAtUtc);
        Assert.Equal(repeatOfId, prescription.RepeatOfPrescriptionId);
    }
}
