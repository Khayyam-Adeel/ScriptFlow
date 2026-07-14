using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPatientRepository
{
    Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Patient patient, CancellationToken cancellationToken = default);

    /// <summary>Case-insensitive match against first name, last name, or NHI.</summary>
    Task<IReadOnlyCollection<Patient>> SearchAsync(string query, CancellationToken cancellationToken = default);

    /// <summary>Bulk lookup for resolving patient names on the prescription list grid without
    /// one request per row (mirrors IMedicineRepository.GetManyAsync).</summary>
    Task<IReadOnlyDictionary<Guid, Patient>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
