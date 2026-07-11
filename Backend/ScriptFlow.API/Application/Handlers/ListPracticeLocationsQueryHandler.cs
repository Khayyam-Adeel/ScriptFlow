using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class ListPracticeLocationsQueryHandler : IRequestHandler<ListPracticeLocationsQuery, IReadOnlyCollection<PracticeLocationDto>>
{
    private readonly IPracticeLocationRepository _practiceLocations;

    public ListPracticeLocationsQueryHandler(IPracticeLocationRepository practiceLocations)
    {
        _practiceLocations = practiceLocations;
    }

    public async Task<IReadOnlyCollection<PracticeLocationDto>> Handle(ListPracticeLocationsQuery request, CancellationToken cancellationToken)
    {
        var locations = await _practiceLocations.ListAsync(request.PracticeId, cancellationToken);
        return locations.Select(l => l.ToDto()).ToList();
    }
}
