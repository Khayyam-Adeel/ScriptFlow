using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using Shared.contract.Enums;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class ProviderTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var practiceLocationId = Guid.NewGuid();
        var provider = new Provider(id, "John", "Smith", ProviderType.Doctor, "NZMC123", practiceLocationId);

        Assert.Equal(id, provider.Id);
        Assert.Equal("John", provider.FirstName);
        Assert.Equal("Smith", provider.LastName);
        Assert.Equal(ProviderType.Doctor, provider.Type);
        Assert.Equal("NZMC123", provider.NzmcNo);
        Assert.Equal(practiceLocationId, provider.PracticeLocationId);
        Assert.Equal("John Smith", provider.FullName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankFirstName_ThrowsDomainException(string? firstName)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), firstName!, "Smith", ProviderType.Doctor, "NZMC123", Guid.NewGuid()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankLastName_ThrowsDomainException(string? lastName)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", lastName!, ProviderType.Doctor, "NZMC123", Guid.NewGuid()));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankNzmcNo_ThrowsDomainException(string? nzmcNo)
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", "Smith", ProviderType.Doctor, nzmcNo!, Guid.NewGuid()));
    }

    [Fact]
    public void Constructor_WithEmptyPracticeLocationId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new Provider(Guid.NewGuid(), "John", "Smith", ProviderType.Doctor, "NZMC123", Guid.Empty));
    }
}
