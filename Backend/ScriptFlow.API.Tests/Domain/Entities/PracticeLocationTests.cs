using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class PracticeLocationTests
{
    private static HpiNumber ValidHpiNumber => new("FZZ99", "B");
    private const string ValidAddress = "1 Test Street, Wellington";
    private const string ValidPhone = "+6421000000";

    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var location = new PracticeLocation(id, practiceId, "Main Clinic", ValidHpiNumber, ValidAddress, ValidPhone);

        Assert.Equal(id, location.Id);
        Assert.Equal(practiceId, location.PracticeId);
        Assert.Equal("Main Clinic", location.Name);
        Assert.Equal("FZZ99-B", location.HpiNumber.Value);
        Assert.Equal(ValidAddress, location.Address);
        Assert.Equal(ValidPhone, location.Phone);
    }

    [Fact]
    public void Constructor_WithEmptyPracticeId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.Empty, "Main Clinic", ValidHpiNumber, ValidAddress, ValidPhone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_ThrowsDomainException(string? name)
    {
        Assert.Throws<DomainException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.NewGuid(), name!, ValidHpiNumber, ValidAddress, ValidPhone));
    }

    [Fact]
    public void Constructor_WithNullHpiNumber_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.NewGuid(), "Main Clinic", null!, ValidAddress, ValidPhone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankAddress_ThrowsDomainException(string? address)
    {
        Assert.Throws<DomainException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.NewGuid(), "Main Clinic", ValidHpiNumber, address!, ValidPhone));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankPhone_ThrowsDomainException(string? phone)
    {
        Assert.Throws<DomainException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.NewGuid(), "Main Clinic", ValidHpiNumber, ValidAddress, phone!));
    }
}
