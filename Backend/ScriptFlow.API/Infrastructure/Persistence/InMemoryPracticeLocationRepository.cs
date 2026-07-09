using System.Collections.Concurrent;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class InMemoryPracticeLocationRepository : IPracticeLocationRepository
{
    private readonly ConcurrentDictionary<Guid, PracticeLocation> _store = new();

    public InMemoryPracticeLocationRepository()
    {
        _store[SeedData.PracticeLocation.Id] = SeedData.PracticeLocation;
    }

    public Task<PracticeLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task AddAsync(PracticeLocation practiceLocation, CancellationToken cancellationToken = default)
    {
        _store[practiceLocation.Id] = practiceLocation;
        return Task.CompletedTask;
    }
}
