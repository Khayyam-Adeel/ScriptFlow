using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class PatientTests
{
    private static Nhi ValidNhi => new("ABC1234");

    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var patient = new Patient(id, "Jane", "Doe", "1 Test Street", ValidNhi);

        Assert.Equal(id, patient.Id);
        Assert.Equal("Jane", patient.FirstName);
        Assert.Equal("Doe", patient.LastName);
        Assert.Equal("1 Test Street", patient.Address);
        Assert.Equal("ABC1234", patient.Nhi.Value);
        Assert.Equal("Jane Doe", patient.FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankFirstName_ThrowsDomainException(string? firstName)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), firstName!, "Doe", "1 Test Street", ValidNhi));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankLastName_ThrowsDomainException(string? lastName)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), "Jane", lastName!, "1 Test Street", ValidNhi));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankAddress_ThrowsDomainException(string? address)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), "Jane", "Doe", address!, ValidNhi));
    }

    [Fact]
    public void Constructor_WithNullNhi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Patient(Guid.NewGuid(), "Jane", "Doe", "1 Test Street", null!));
    }
}
