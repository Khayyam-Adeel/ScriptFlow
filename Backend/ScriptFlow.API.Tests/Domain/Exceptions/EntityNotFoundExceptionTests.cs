using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Tests.Domain.Exceptions;

public class EntityNotFoundExceptionTests
{
    [Fact]
    public void Constructor_FormatsMessageWithEntityNameAndKey()
    {
        var id = Guid.NewGuid();

        var exception = new EntityNotFoundException("Patient", id);

        Assert.Equal($"Patient with id '{id}' was not found.", exception.Message);
        Assert.IsAssignableFrom<DomainException>(exception);
    }
}
