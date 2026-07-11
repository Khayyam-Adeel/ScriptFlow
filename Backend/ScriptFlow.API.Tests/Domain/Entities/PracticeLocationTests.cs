using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class PracticeLocationTests
{
    private static HpiNumber ValidHpiNumber => new("FZZ99", "B");

    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var practiceId = Guid.NewGuid();
        var location = new PracticeLocation(id, practiceId, "Main Clinic", ValidHpiNumber);

        Assert.Equal(id, location.Id);
        Assert.Equal(practiceId, location.PracticeId);
        Assert.Equal("Main Clinic", location.Name);
        Assert.Equal("FZZ99-B", location.HpiNumber.Value);
    }

    [Fact]
    public void Constructor_WithEmptyPracticeId_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.Empty, "Main Clinic", ValidHpiNumber));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_ThrowsDomainException(string? name)
    {
        Assert.Throws<DomainException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.NewGuid(), name!, ValidHpiNumber));
    }

    [Fact]
    public void Constructor_WithNullHpiNumber_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new PracticeLocation(Guid.NewGuid(), Guid.NewGuid(), "Main Clinic", null!));
    }
}
