using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Tests.Domain.Entities;

public class SystemUserTests
{
    [Fact]
    public void Id_IsAWellKnownNonEmptyGuid()
    {
        Assert.NotEqual(Guid.Empty, SystemUser.Id);
        Assert.Equal(Guid.Parse("00000000-0000-0000-0000-0000000000AA"), SystemUser.Id);
    }
}
