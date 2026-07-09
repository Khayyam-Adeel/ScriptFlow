using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive match against first name, last name, or NHI.</summary>
    Task<IReadOnlyCollection<Patient>> SearchAsync(string query, CancellationToken cancellationToken = default);
}
