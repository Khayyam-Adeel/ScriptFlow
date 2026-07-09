using MediatR;
using ScriptFlow.API.Application.DTOs;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Queries;

public sealed record ListPrescriptionsQuery(Guid? PatientId, PrescriptionStatus? Status) : IRequest<IReadOnlyCollection<PrescriptionDto>>;
