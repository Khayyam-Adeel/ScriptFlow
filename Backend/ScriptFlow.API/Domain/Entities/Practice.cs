using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>Minimal record of a practice (clinic/organisation) that owns one or more locations.</summary>
public sealed class Practice
{
    public Guid Id { get; }
    public string Name { get; }

    public Practice(Guid id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("Practice name is required.");
        }

        Id = id;
        Name = name;
    }
}
