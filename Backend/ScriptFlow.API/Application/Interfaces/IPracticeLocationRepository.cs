using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPracticeLocationRepository
{
    Task<PracticeLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(PracticeLocation practiceLocation, CancellationToken cancellationToken = default);
}
