using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class PracticeTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var practice = new Practice(id, "Test Practice");

        Assert.Equal(id, practice.Id);
        Assert.Equal("Test Practice", practice.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_ThrowsDomainException(string? name)
    {
        Assert.Throws<DomainException>(() => new Practice(Guid.NewGuid(), name!));
    }
}
