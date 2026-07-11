using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPracticeLocationRepository
{
    Task<PracticeLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(PracticeLocation practiceLocation, CancellationToken cancellationToken = default);

    /// <summary>Lists active practice locations, optionally filtered to one practice, for provider/prescription pickers.</summary>
    Task<IReadOnlyCollection<PracticeLocation>> ListAsync(Guid? practiceId, CancellationToken cancellationToken = default);
}
