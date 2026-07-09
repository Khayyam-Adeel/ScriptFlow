using System.Collections.Concurrent;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using Shared.contract.Enums;

namespace ScriptFlow.API.Infrastructure.Persistence;

/// <summary>Thread-safe in-memory store. Data is lost on restart; a real EF Core + SQL Server
/// implementation is a follow-up pass once DatabaseSpec.md is filled in.</summary>
public sealed class InMemoryPrescriptionRepository : IPrescriptionRepository
{
    private readonly ConcurrentDictionary<Guid, Prescription> _store = new();

    public Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.GetValueOrDefault(id));

    public Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        _store[prescription.Id] = prescription;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        _store[prescription.Id] = prescription;
        return Task.CompletedTask;
    }

    public Task<IReadOnlyCollection<Prescription>> ListAsync(
        Guid? patientId, PrescriptionStatus? status, CancellationToken cancellationToken = default)
    {
        IEnumerable<Prescription> results = _store.Values;

        if (patientId is not null)
        {
            results = results.Where(p => p.PatientId == patientId);
        }

        if (status is not null)
        {
            results = results.Where(p => p.Status == status);
        }

        return Task.FromResult<IReadOnlyCollection<Prescription>>(
            results.OrderByDescending(p => p.CreatedAtUtc).ToList());
    }
}
