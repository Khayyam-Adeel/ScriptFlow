using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, ProviderDto>
{
    private readonly IProviderRepository _providers;
    private readonly IPracticeLocationRepository _practiceLocations;

    public CreateProviderCommandHandler(IProviderRepository providers, IPracticeLocationRepository practiceLocations)
    {
        _providers = providers;
        _practiceLocations = practiceLocations;
    }

    public async Task<ProviderDto> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
    {
        _ = await _practiceLocations.GetByIdAsync(request.PracticeLocationId, cancellationToken)
            ?? throw new EntityNotFoundException("PracticeLocation", request.PracticeLocationId);

        var provider = new Provider(
            Guid.NewGuid(), request.FirstName, request.LastName, request.Type, request.NzmcNo, request.PracticeLocationId,
            request.Email, request.PhoneNumber, request.Qualification);

        await _providers.AddAsync(provider, cancellationToken);
        return provider.ToDto();
    }
}
