using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Application.Handlers;

public sealed class CreatePracticeLocationCommandHandler : IRequestHandler<CreatePracticeLocationCommand, PracticeLocationDto>
{
    private readonly IPracticeLocationRepository _practiceLocations;
    private readonly IPracticeRepository _practices;

    public CreatePracticeLocationCommandHandler(IPracticeLocationRepository practiceLocations, IPracticeRepository practices)
    {
        _practiceLocations = practiceLocations;
        _practices = practices;
    }

    public async Task<PracticeLocationDto> Handle(CreatePracticeLocationCommand request, CancellationToken cancellationToken)
    {
        _ = await _practices.GetByIdAsync(request.PracticeId, cancellationToken)
            ?? throw new EntityNotFoundException("Practice", request.PracticeId);

        var location = new PracticeLocation(
            Guid.NewGuid(), request.PracticeId, request.Name,
            new HpiNumber(request.HpiNo, request.HpiExtension), request.Address, request.Phone);

        await _practiceLocations.AddAsync(location, cancellationToken);
        return location.ToDto();
    }
}
