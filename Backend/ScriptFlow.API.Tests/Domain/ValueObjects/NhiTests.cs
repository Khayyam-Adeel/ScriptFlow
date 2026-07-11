using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Tests.Domain.ValueObjects;

public class NhiTests
{
    [Fact]
    public void Constructor_WithValidFormat_SetsValue()
    {
        var nhi = new Nhi("ABC1234");

        Assert.Equal("ABC1234", nhi.Value);
    }

    [Fact]
    public void Constructor_LowercaseWithWhitespace_IsNormalized()
    {
        var nhi = new Nhi("  abc1234  ");

        Assert.Equal("ABC1234", nhi.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("AB1234")]   // only 2 letters
    [InlineData("ABCD1234")] // 4 letters
    [InlineData("ABC123")]   // only 3 digits
    [InlineData("ABC12345")] // 5 digits
    [InlineData("1234ABC")]  // wrong order
    public void Constructor_WithInvalidFormat_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => new Nhi(value!));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var nhi = new Nhi("ABC1234");

        Assert.Equal(nhi.Value, nhi.ToString());
    }
}
