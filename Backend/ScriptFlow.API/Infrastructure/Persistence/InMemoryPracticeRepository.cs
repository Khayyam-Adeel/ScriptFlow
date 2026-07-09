using System.Collections.Concurrent;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class InMemoryPracticeRepository : IPracticeRepository
{
    private readonly ConcurrentDictionary<Guid, Practice> _store = new();

    public InMemoryPracticeRepository()
    {
        _store[SeedData.Practice.Id] = SeedData.Practice;
    }

    public Task<Practice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task AddAsync(Practice practice, CancellationToken cancellationToken = default)
    {
        _store[practice.Id] = practice;
        return Task.CompletedTask;
    }
}
