using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IProviderRepository
{
    Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Provider provider, CancellationToken cancellationToken = default);
}
