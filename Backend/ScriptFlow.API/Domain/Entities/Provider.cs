using ScriptFlow.API.Domain.Exceptions;
using Shared.contract.Enums;

namespace ScriptFlow.API.Domain.Entities;

public sealed class Provider
{
    public Guid Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public ProviderType Type { get; }
    public string NzmcNo { get; }
    public Guid PracticeLocationId { get; }
    public string Email { get; }
    public string PhoneNumber { get; }
    public string Qualification { get; }

    public string FullName => $"{FirstName} {LastName}";

    public Provider(
        Guid id,
        string firstName,
        string lastName,
        ProviderType type,
        string nzmcNo,
        Guid practiceLocationId,
        string email,
        string phoneNumber,
        string qualification)
    {
        if (string.IsNullOrWhiteSpace(firstName))
        {
            throw new DomainException("Provider first name is required.");
        }

        if (string.IsNullOrWhiteSpace(lastName))
        {
            throw new DomainException("Provider last name is required.");
        }

        if (string.IsNullOrWhiteSpace(nzmcNo))
        {
            throw new DomainException("Provider NZMC no is required.");
        }

        if (practiceLocationId == Guid.Empty)
        {
            throw new DomainException("Provider must be linked to a practice location.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new DomainException("Provider email is required.");
        }

        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new DomainException("Provider phone number is required.");
        }

        if (string.IsNullOrWhiteSpace(qualification))
        {
            throw new DomainException("Provider qualification is required.");
        }

        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Type = type;
        NzmcNo = nzmcNo;
        PracticeLocationId = practiceLocationId;
        Email = email;
        PhoneNumber = phoneNumber;
        Qualification = qualification;
    }
}
