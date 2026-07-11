using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class UserTests
{
    [Fact]
    public void Constructor_WithValidArguments_SetsProperties()
    {
        var id = Guid.NewGuid();
        var user = new User(id, "Jane.Doe@Example.com", "hashed-password");

        Assert.Equal(id, user.Id);
        Assert.Equal("jane.doe@example.com", user.Email);
        Assert.Equal("hashed-password", user.PasswordHash);
    }

    [Fact]
    public void Constructor_TrimsAndLowercasesEmail()
    {
        var user = new User(Guid.NewGuid(), "  MixedCase@Example.com  ", "hashed-password");

        Assert.Equal("mixedcase@example.com", user.Email);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankEmail_ThrowsDomainException(string? email)
    {
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), email!, "hashed-password"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankPasswordHash_ThrowsDomainException(string? passwordHash)
    {
        Assert.Throws<DomainException>(() => new User(Guid.NewGuid(), "jane.doe@example.com", passwordHash!));
    }
}
