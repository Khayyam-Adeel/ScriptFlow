using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Domain.Entities;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task UpdateAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Prescription>> ListAsync(Guid? patientId, PrescriptionStatus? status, CancellationToken cancellationToken = default);

    /// <summary>Counts across every prescription (a cheap GROUP BY), not a fetch-everything-then-
    /// count-client-side - ListAsync is capped to the 200 most recent matches and would
    /// undercount once the table has more rows than that.</summary>
    Task<IReadOnlyCollection<PrescriptionStatusCountDto>> GetStatusCountsAsync(CancellationToken cancellationToken = default);
}
