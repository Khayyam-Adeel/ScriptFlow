using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Tests.Domain.ValueObjects;

public class ScidTests
{
    [Theory]
    [InlineData("9J0BGVA1B2C")]
    [InlineData("9AAAAABBBBB")]
    public void Constructor_WithValidFormat_SetsUppercasedValue(string value)
    {
        var scid = new Scid(value);

        Assert.Equal(value.ToUpperInvariant(), scid.Value);
    }

    [Fact]
    public void Constructor_LowercaseInput_IsUppercased()
    {
        var scid = new Scid("9j0bgva1b2c");

        Assert.Equal("9J0BGVA1B2C", scid.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("8J0BGVA1B2C")] // doesn't start with 9
    [InlineData("9J0BGVA1B2")]  // too short
    [InlineData("9J0BGVA1B2CC")] // too long
    public void Constructor_WithInvalidFormat_ThrowsDomainException(string? value)
    {
        Assert.Throws<DomainException>(() => new Scid(value!));
    }

    [Fact]
    public void Generate_ProducesValidScid()
    {
        var scid = Scid.Generate();

        Assert.StartsWith("9", scid.Value);
        Assert.Equal(11, scid.Value.Length);
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var scid = new Scid("9J0BGVA1B2C");

        Assert.Equal(scid.Value, scid.ToString());
    }
}
