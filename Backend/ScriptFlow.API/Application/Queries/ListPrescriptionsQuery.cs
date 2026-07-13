using MediatR;
using ScriptFlow.API.Application.DTOs;
using Shared.contract.Enums;

namespace ScriptFlow.API.Application.Queries;

public sealed record ListPrescriptionsQuery(
    Guid? PatientId,
    Guid? ProviderId,
    PrescriptionStatus? Status,
    string? ScidPrefix,
    DateTime? CreatedFrom,
    DateTime? CreatedTo) : IRequest<IReadOnlyCollection<PrescriptionDto>>;
