namespace ScriptFlow.API.Domain.Exceptions;

public sealed class InvalidPrescriptionStateException : DomainException
{
    public InvalidPrescriptionStateException(string message) : base(message)
    {
    }
}
