using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetRejectionRateByProviderQueryHandler : IRequestHandler<GetRejectionRateByProviderQuery, IReadOnlyCollection<RejectionRateDto>>
{
    private readonly IPrescriptionRepository _prescriptions;

    public GetRejectionRateByProviderQueryHandler(IPrescriptionRepository prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public Task<IReadOnlyCollection<RejectionRateDto>> Handle(GetRejectionRateByProviderQuery request, CancellationToken cancellationToken) =>
        _prescriptions.GetRejectionRateByProviderAsync(cancellationToken);
}
