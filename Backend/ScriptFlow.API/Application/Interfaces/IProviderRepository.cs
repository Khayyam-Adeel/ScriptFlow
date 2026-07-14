using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Provider provider, CancellationToken cancellationToken = default);

    /// <summary>Lists active providers, optionally filtered to one practice location, for prescription pickers.</summary>
    Task<IReadOnlyCollection<Provider>> ListAsync(Guid? practiceLocationId, CancellationToken cancellationToken = default);

    /// <summary>Bulk lookup for resolving provider names on the prescription list grid without
    /// one request per row (mirrors IMedicineRepository.GetManyAsync).</summary>
    Task<IReadOnlyDictionary<Guid, Provider>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
}
