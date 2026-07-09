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

    public string FullName => $"{FirstName} {LastName}";

    public Provider(Guid id, string firstName, string lastName, ProviderType type, string nzmcNo, Guid practiceLocationId)
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

        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Type = type;
        NzmcNo = nzmcNo;
        PracticeLocationId = practiceLocationId;
    }
}
