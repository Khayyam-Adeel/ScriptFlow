namespace ScriptFlow.API.Domain.Exceptions;

/// <summary>Thrown when SqlPatientRepository.AddAsync hits UQ_Patients_Nhi - translates a raw SQL
/// unique-constraint violation into a message a caller can actually act on.</summary>
public sealed class DuplicateNhiException : DomainException
{
    public DuplicateNhiException(string nhi)
        : base($"A patient with NHI '{nhi}' already exists.")
    {
    }
}
