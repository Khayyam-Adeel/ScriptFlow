using System.Collections.Concurrent;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class InMemoryMedicineRepository : IMedicineRepository
{
    private readonly ConcurrentDictionary<Guid, Medicine> _store = new();

    public InMemoryMedicineRepository()
    {
        foreach (var medicine in SeedData.Medicines)
        {
            _store[medicine.Id] = medicine;
        }
    }

    public Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task<IReadOnlyDictionary<Guid, Medicine>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.ToHashSet();
        var result = _store.Values.Where(m => idSet.Contains(m.Id)).ToDictionary(m => m.Id, m => m);
        return Task.FromResult<IReadOnlyDictionary<Guid, Medicine>>(result);
    }
}
