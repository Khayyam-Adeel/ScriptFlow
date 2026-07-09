using ScriptFlow.API.Domain.Entities;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Interfaces;

public interface IPrescriptionRepository
{
    Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task UpdateAsync(Prescription prescription, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Prescription>> ListAsync(Guid? patientId, PrescriptionStatus? status, CancellationToken cancellationToken = default);
}
