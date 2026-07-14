using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.contract.Enums;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class PatientTests
{
    private static Nhi ValidNhi => new("ABC1234");
    private static DateOnly ValidDateOfBirth => new(1990, 1, 1);
    private const Gender ValidGender = Gender.Female;
    private const string ValidPhoneNumber = "0271234567";
    private const string ValidEmail = "jane.doe@example.com";

    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var patient = new Patient(
            id, "Jane", "Doe", "1 Test Street", ValidNhi, ValidDateOfBirth, ValidGender, ValidPhoneNumber, ValidEmail);

        Assert.Equal(id, patient.Id);
        Assert.Equal("Jane", patient.FirstName);
        Assert.Equal("Doe", patient.LastName);
        Assert.Equal("1 Test Street", patient.Address);
        Assert.Equal("ABC1234", patient.Nhi.Value);
        Assert.Equal(ValidDateOfBirth, patient.DateOfBirth);
        Assert.Equal(ValidGender, patient.Gender);
        Assert.Equal(ValidPhoneNumber, patient.PhoneNumber);
        Assert.Equal(ValidEmail, patient.Email);
        Assert.Equal("Jane Doe", patient.FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankFirstName_ThrowsDomainException(string? firstName)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), firstName!, "Doe", "1 Test Street", ValidNhi, ValidDateOfBirth, ValidGender, ValidPhoneNumber, ValidEmail));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankLastName_ThrowsDomainException(string? lastName)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), "Jane", lastName!, "1 Test Street", ValidNhi, ValidDateOfBirth, ValidGender, ValidPhoneNumber, ValidEmail));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankAddress_ThrowsDomainException(string? address)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), "Jane", "Doe", address!, ValidNhi, ValidDateOfBirth, ValidGender, ValidPhoneNumber, ValidEmail));
    }

    [Fact]
    public void Constructor_WithNullNhi_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new Patient(Guid.NewGuid(), "Jane", "Doe", "1 Test Street", null!, ValidDateOfBirth, ValidGender, ValidPhoneNumber, ValidEmail));
    }

    [Fact]
    public void Constructor_WithFutureDateOfBirth_ThrowsDomainException()
    {
        var futureDob = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));

        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), "Jane", "Doe", "1 Test Street", ValidNhi, futureDob, ValidGender, ValidPhoneNumber, ValidEmail));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankPhoneNumber_ThrowsDomainException(string? phoneNumber)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), "Jane", "Doe", "1 Test Street", ValidNhi, ValidDateOfBirth, ValidGender, phoneNumber!, ValidEmail));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankEmail_ThrowsDomainException(string? email)
    {
        Assert.Throws<DomainException>(() =>
            new Patient(Guid.NewGuid(), "Jane", "Doe", "1 Test Street", ValidNhi, ValidDateOfBirth, ValidGender, ValidPhoneNumber, email!));
    }
}
