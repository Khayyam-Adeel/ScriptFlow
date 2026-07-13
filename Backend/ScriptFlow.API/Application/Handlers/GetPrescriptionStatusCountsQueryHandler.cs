using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetPrescriptionStatusCountsQueryHandler
    : IRequestHandler<GetPrescriptionStatusCountsQuery, IReadOnlyCollection<PrescriptionStatusCountDto>>
{
    private readonly IPrescriptionRepository _prescriptions;

    public GetPrescriptionStatusCountsQueryHandler(IPrescriptionRepository prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public Task<IReadOnlyCollection<PrescriptionStatusCountDto>> Handle(
        GetPrescriptionStatusCountsQuery request, CancellationToken cancellationToken)
        => _prescriptions.GetStatusCountsAsync(cancellationToken);
}
