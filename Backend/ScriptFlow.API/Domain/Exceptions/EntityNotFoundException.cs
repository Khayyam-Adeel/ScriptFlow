namespace ScriptFlow.API.Domain.Exceptions;

public sealed class EntityNotFoundException : DomainException
{
    public EntityNotFoundException(string entityName, object key)
        : base($"{entityName} with id '{key}' was not found.")
    {
    }
}
