using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IMedicineRepository
{
    Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, Medicine>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>Lists active medicines, optionally filtered by a name/SCTID search term, for medication-line pickers.</summary>
    Task<IReadOnlyCollection<Medicine>> ListAsync(string? search, CancellationToken cancellationToken = default);
}
