using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetRejectionRateByLocationQueryHandler : IRequestHandler<GetRejectionRateByLocationQuery, IReadOnlyCollection<RejectionRateDto>>
{
    private readonly IPrescriptionRepository _prescriptions;

    public GetRejectionRateByLocationQueryHandler(IPrescriptionRepository prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public Task<IReadOnlyCollection<RejectionRateDto>> Handle(GetRejectionRateByLocationQuery request, CancellationToken cancellationToken) =>
        _prescriptions.GetRejectionRateByLocationAsync(cancellationToken);
}
