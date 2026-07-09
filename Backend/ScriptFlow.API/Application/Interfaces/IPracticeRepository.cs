using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPracticeRepository
{
    Task<Practice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Practice practice, CancellationToken cancellationToken = default);
}
