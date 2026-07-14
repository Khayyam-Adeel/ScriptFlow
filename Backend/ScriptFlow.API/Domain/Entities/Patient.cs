using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.contract.Enums;

namespace ScriptFlow.API.Domain.Entities;

public sealed class Patient
{
    public Guid Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Address { get; }
    public Nhi Nhi { get; }
    public DateOnly DateOfBirth { get; }
    public Gender Gender { get; }
    public string PhoneNumber { get; }
    public string Email { get; }

    public string FullName => $"{FirstName} {LastName}";

    public Patient(
        Guid id,
        string firstName,
        string lastName,
        string address,
        Nhi nhi,
        DateOnly dateOfBirth,
        Gender gender,
        string phoneNumber,
        string email)
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

        if (dateOfBirth > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw new DomainException("Patient date of birth cannot be in the future.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Patient phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Patient email is required.");
        }

        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Address = address;
        Nhi = nhi ?? throw new ArgumentNullException(nameof(nhi));
        DateOfBirth = dateOfBirth;
        Gender = gender;
        PhoneNumber = phoneNumber;
        Email = email;
    }
}
