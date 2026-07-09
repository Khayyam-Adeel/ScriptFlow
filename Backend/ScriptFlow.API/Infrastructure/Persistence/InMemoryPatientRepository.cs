using System.Collections.Concurrent;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class InMemoryPatientRepository : IPatientRepository
{
    private readonly ConcurrentDictionary<Guid, Patient> _store = new();

    public Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        _store[patient.Id] = patient;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Patient>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var normalized = query?.Trim() ?? string.Empty;

        var results = _store.Values.Where(p =>
            p.FirstName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
            p.LastName.Contains(normalized, StringComparison.OrdinalIgnoreCase) ||
            p.Nhi.Value.Contains(normalized, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult<IReadOnlyCollection<Patient>>(results.ToList());
    }
}
