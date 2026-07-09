using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>Master-data record for a medicine, identified by its real-world SNOMED CT code.</summary>
public sealed class Medicine
{
    public Guid Id { get; }
    public string Name { get; }
    public string Sctid { get; }
    public string Form { get; }

    public Medicine(Guid id, string name, string sctid, string form)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Medicine name is required.");
        }

        if (string.IsNullOrWhiteSpace(sctid))
        {
            throw new DomainException("Medicine SCTID is required.");
        }

        if (string.IsNullOrWhiteSpace(form))
        {
            throw new DomainException("Medicine form is required.");
        }

        Id = id;
        Name = name;
        Sctid = sctid;
        Form = form;
    }
}
