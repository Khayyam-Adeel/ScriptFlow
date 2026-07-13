using MediatR;
using ScriptFlow.API.Application.DTOs;

namespace ScriptFlow.API.Application.Queries;

/// <summary>For the dashboard's "prescriptions by status" tiles - counts across every
/// prescription, not just a page of them (see IPrescriptionRepository.GetStatusCountsAsync).</summary>
public sealed record GetPrescriptionStatusCountsQuery : IRequest<IReadOnlyCollection<PrescriptionStatusCountDto>>;
