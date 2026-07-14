using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetVolumeByLocationQueryHandler : IRequestHandler<GetVolumeByLocationQuery, IReadOnlyCollection<LocationVolumeDto>>
{
    private readonly IPrescriptionRepository _prescriptions;

    public GetVolumeByLocationQueryHandler(IPrescriptionRepository prescriptions)
    {
        _prescriptions = prescriptions;
    }

    public Task<IReadOnlyCollection<LocationVolumeDto>> Handle(GetVolumeByLocationQuery request, CancellationToken cancellationToken) =>
        _prescriptions.GetVolumeByLocationAsync(cancellationToken);
}
