using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>A physical location of a <see cref="Practice"/>, identified by its HPI number.</summary>
public sealed class PracticeLocation
{
    public Guid Id { get; }
    public Guid PracticeId { get; }
    public string Name { get; }
    public HpiNumber HpiNumber { get; }
    public string Address { get; }
    public string Phone { get; }

    public PracticeLocation(Guid id, Guid practiceId, string name, HpiNumber hpiNumber, string address, string phone)
    {
        if (practiceId == Guid.Empty)
        {
            throw new DomainException("Practice location must belong to a practice.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Practice location name is required.");
        }

        if (string.IsNullOrWhiteSpace(address))
        {
            throw new DomainException("Practice location address is required.");
        }

        if (string.IsNullOrWhiteSpace(phone))
        {
            throw new DomainException("Practice location phone is required.");
        }

        Id = id;
        PracticeId = practiceId;
        Name = name;
        HpiNumber = hpiNumber ?? throw new ArgumentNullException(nameof(hpiNumber));
        Address = address;
        Phone = phone;
    }
}
