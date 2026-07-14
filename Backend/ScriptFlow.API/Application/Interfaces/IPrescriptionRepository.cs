using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Domain.Entities;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task UpdateAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Prescription>> ListAsync(
        Guid? patientId, Guid? providerId, PrescriptionStatus? status, string? scidPrefix,
        DateTime? createdFrom, DateTime? createdToExclusive, CancellationToken cancellationToken = default);

    /// <summary>Counts across every prescription (a cheap GROUP BY), not a fetch-everything-then-
    /// count-client-side - ListAsync is capped to the 200 most recent matches and would
    /// undercount once the table has more rows than that.</summary>
    Task<IReadOnlyCollection<PrescriptionStatusCountDto>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>Prescriptions created per day for the dashboard's volume trend chart, bounded to
    /// a recent window (sinceUtc) so it stays a cheap indexed range scan even at 1M+ rows -
    /// unlike GetStatusCountsAsync this isn't a full-table GROUP BY.</summary>
    Task<IReadOnlyCollection<PrescriptionDailyVolumeDto>> GetDailyVolumeAsync(DateTime sinceUtc, CancellationToken cancellationToken = default);

    /// <summary>Full aggregates (not capped like ListAsync) for every prescription still in a
    /// non-terminal state (Created, Signed, or Dispatched) created before olderThanUtc - what
    /// PrescriptionExpiryService's periodic sweep needs to actually call .Expire() on each one,
    /// not just count them.</summary>
    Task<IReadOnlyCollection<Prescription>> GetStaleForExpiryAsync(DateTime olderThanUtc, CancellationToken cancellationToken = default);

    /// <summary>Admin overview chart: total prescription volume (all statuses) per practice
    /// location, top 20 by volume.</summary>
    Task<IReadOnlyCollection<LocationVolumeDto>> GetVolumeByLocationAsync(CancellationToken cancellationToken = default);

    /// <summary>Admin overview chart: rejection rate of finalized (Acknowledged/Rejected)
    /// prescriptions per practice location.</summary>
    Task<IReadOnlyCollection<RejectionRateDto>> GetRejectionRateByLocationAsync(CancellationToken cancellationToken = default);

    /// <summary>Admin overview chart: rejection rate of finalized prescriptions per provider,
    /// top 10 by rate.</summary>
    Task<IReadOnlyCollection<RejectionRateDto>> GetRejectionRateByProviderAsync(CancellationToken cancellationToken = default);
}
