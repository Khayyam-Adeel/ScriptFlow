using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using Shared.contract.Enums;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class ProviderTests
{
    private const string ValidEmail = "john.smith@example.com";
    private const string ValidPhoneNumber = "0271234567";
    private const string ValidQualification = "MBBS";

    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var practiceLocationId = Guid.NewGuid();
        var provider = new Provider(
            id, "John", "Smith", ProviderType.Doctor, "NZMC123", practiceLocationId,
            ValidEmail, ValidPhoneNumber, ValidQualification);

        Assert.Equal(id, provider.Id);
        Assert.Equal("John", provider.FirstName);
        Assert.Equal("Smith", provider.LastName);
        Assert.Equal(ProviderType.Doctor, provider.Type);
        Assert.Equal("NZMC123", provider.NzmcNo);
        Assert.Equal(practiceLocationId, provider.PracticeLocationId);
        Assert.Equal(ValidEmail, provider.Email);
        Assert.Equal(ValidPhoneNumber, provider.PhoneNumber);
        Assert.Equal(ValidQualification, provider.Qualification);
        Assert.Equal("John Smith", provider.FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankFirstName_ThrowsDomainException(string? firstName)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), firstName!, "Smith", ProviderType.Doctor, "NZMC123", Guid.NewGuid(),
                ValidEmail, ValidPhoneNumber, ValidQualification));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankLastName_ThrowsDomainException(string? lastName)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", lastName!, ProviderType.Doctor, "NZMC123", Guid.NewGuid(),
                ValidEmail, ValidPhoneNumber, ValidQualification));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankNzmcNo_ThrowsDomainException(string? nzmcNo)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", "Smith", ProviderType.Doctor, nzmcNo!, Guid.NewGuid(),
                ValidEmail, ValidPhoneNumber, ValidQualification));
    }

    [Fact]
    public void Constructor_WithEmptyPracticeLocationId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", "Smith", ProviderType.Doctor, "NZMC123", Guid.Empty,
                ValidEmail, ValidPhoneNumber, ValidQualification));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankEmail_ThrowsDomainException(string? email)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", "Smith", ProviderType.Doctor, "NZMC123", Guid.NewGuid(),
                email!, ValidPhoneNumber, ValidQualification));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankPhoneNumber_ThrowsDomainException(string? phoneNumber)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", "Smith", ProviderType.Doctor, "NZMC123", Guid.NewGuid(),
                ValidEmail, phoneNumber!, ValidQualification));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankQualification_ThrowsDomainException(string? qualification)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", "Smith", ProviderType.Doctor, "NZMC123", Guid.NewGuid(),
                ValidEmail, ValidPhoneNumber, qualification!));
    }
}
