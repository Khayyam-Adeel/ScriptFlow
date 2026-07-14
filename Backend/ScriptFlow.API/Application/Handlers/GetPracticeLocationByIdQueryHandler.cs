using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetPracticeLocationByIdQueryHandler : IRequestHandler<GetPracticeLocationByIdQuery, PracticeLocationDto>
{
    private readonly IPracticeLocationRepository _practiceLocations;

    public GetPracticeLocationByIdQueryHandler(IPracticeLocationRepository practiceLocations)
    {
        _practiceLocations = practiceLocations;
    }

    public async Task<PracticeLocationDto> Handle(GetPracticeLocationByIdQuery request, CancellationToken cancellationToken)
    {
        var location = await _practiceLocations.GetByIdAsync(request.PracticeLocationId, cancellationToken)
            ?? throw new EntityNotFoundException("PracticeLocation", request.PracticeLocationId);
        return location.ToDto();
    }
}
