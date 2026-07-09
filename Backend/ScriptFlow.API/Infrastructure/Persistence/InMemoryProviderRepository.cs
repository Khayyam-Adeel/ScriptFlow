using System.Collections.Concurrent;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class InMemoryProviderRepository : IProviderRepository
{
    private readonly ConcurrentDictionary<Guid, Provider> _store = new();

    public Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task AddAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        _store[provider.Id] = provider;
        return Task.CompletedTask;
    }
}
