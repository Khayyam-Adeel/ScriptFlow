using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Domain.Entities;

public sealed class Patient
{
    public Guid Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Address { get; }
    public Nhi Nhi { get; }

    public string FullName => $"{FirstName} {LastName}";

    public Patient(Guid id, string firstName, string lastName, string address, Nhi nhi)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("Patient first name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Patient last name is required.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new DomainException("Patient address is required.");
        }

        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Address = address;
        Nhi = nhi ?? throw new ArgumentNullException(nameof(nhi));
    }
}
