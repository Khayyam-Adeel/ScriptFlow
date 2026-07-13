using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>For the dashboard's volume trend chart - prescriptions created per day over a fixed
/// recent window (see GetPrescriptionDailyVolumeQueryHandler), not the whole table (see
/// IPrescriptionRepository.GetDailyVolumeAsync).</summary>
public sealed record GetPrescriptionDailyVolumeQuery : IRequest<IReadOnlyCollection<PrescriptionDailyVolumeDto>>;
