using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Tests.Domain.ValueObjects;

public class HpiNumberTests
{
    [Fact]
    public void Constructor_WithValidParts_SetsCombinedValue()
    {
        var hpi = new HpiNumber("FZZ99", "B");

        Assert.Equal("FZZ99", hpi.HpiNo);
        Assert.Equal("B", hpi.HpiExtension);
        Assert.Equal("FZZ99-B", hpi.Value);
    }

    [Fact]
    public void Constructor_LowercaseWithWhitespace_IsNormalized()
    {
        var hpi = new HpiNumber("  fzz99  ", " b ");

        Assert.Equal("FZZ99-B", hpi.Value);
    }

    [Theory]
    [InlineData(null, "B")]
    [InlineData("", "B")]
    [InlineData("FZ99", "B")]   // only 2 letters
    [InlineData("FZZZ99", "B")] // 4 letters
    [InlineData("FZZ9", "B")]   // only 1 digit
    [InlineData("FZZ999", "B")] // 3 digits
    public void Constructor_WithInvalidHpiNo_ThrowsDomainException(string? hpiNo, string extension)
    {
        Assert.Throws<DomainException>(() => new HpiNumber(hpiNo!, extension));
    }

    [Theory]
    [InlineData("FZZ99", null)]
    [InlineData("FZZ99", "")]
    [InlineData("FZZ99", "BB")]
    [InlineData("FZZ99", "1")]
    public void Constructor_WithInvalidExtension_ThrowsDomainException(string hpiNo, string? extension)
    {
        Assert.Throws<DomainException>(() => new HpiNumber(hpiNo, extension!));
    }

    [Fact]
    public void Parse_WithValidCombinedString_ReturnsHpiNumber()
    {
        var hpi = HpiNumber.Parse("FZZ99-B");

        Assert.Equal("FZZ99-B", hpi.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("FZZ99")]        // no separator
    [InlineData("FZZ99-B-EXTRA")] // too many parts
    public void Parse_WithInvalidCombinedString_ThrowsDomainException(string? combined)
    {
        Assert.Throws<DomainException>(() => HpiNumber.Parse(combined!));
    }

    [Fact]
    public void ToString_ReturnsValue()
    {
        var hpi = new HpiNumber("FZZ99", "B");

        Assert.Equal(hpi.Value, hpi.ToString());
    }
}
