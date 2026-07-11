using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class PrescriptionMedicationTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var medicineId = Guid.NewGuid();
        var medication = new PrescriptionMedication(id, medicineId, "500mg", "Twice daily", "7 days", 14, "Take with food");

        Assert.Equal(id, medication.Id);
        Assert.Equal(medicineId, medication.MedicineId);
        Assert.Equal("500mg", medication.TakeValue);
        Assert.Equal("Twice daily", medication.Frequency);
        Assert.Equal("7 days", medication.Duration);
        Assert.Equal(14, medication.Quantity);
        Assert.Equal("Take with food", medication.Directions);
    }

    [Fact]
    public void Constructor_WithEmptyMedicineId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new PrescriptionMedication(Guid.NewGuid(), Guid.Empty, "500mg", "Twice daily", "7 days", 14, "Take with food"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankTakeValue_ThrowsDomainException(string? takeValue)
    {
        Assert.Throws<DomainException>(() =>
            new PrescriptionMedication(Guid.NewGuid(), Guid.NewGuid(), takeValue!, "Twice daily", "7 days", 14, "Take with food"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankFrequency_ThrowsDomainException(string? frequency)
    {
        Assert.Throws<DomainException>(() =>
            new PrescriptionMedication(Guid.NewGuid(), Guid.NewGuid(), "500mg", frequency!, "7 days", 14, "Take with food"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankDuration_ThrowsDomainException(string? duration)
    {
        Assert.Throws<DomainException>(() =>
            new PrescriptionMedication(Guid.NewGuid(), Guid.NewGuid(), "500mg", "Twice daily", duration!, 14, "Take with food"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositiveQuantity_ThrowsDomainException(int quantity)
    {
        Assert.Throws<DomainException>(() =>
            new PrescriptionMedication(Guid.NewGuid(), Guid.NewGuid(), "500mg", "Twice daily", "7 days", quantity, "Take with food"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankDirections_ThrowsDomainException(string? directions)
    {
        Assert.Throws<DomainException>(() =>
            new PrescriptionMedication(Guid.NewGuid(), Guid.NewGuid(), "500mg", "Twice daily", "7 days", 14, directions!));
    }
}
