using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetProviderByIdQueryHandler : IRequestHandler<GetProviderByIdQuery, ProviderDto>
{
    private readonly IProviderRepository _providers;

    public GetProviderByIdQueryHandler(IProviderRepository providers)
    {
        _providers = providers;
    }

    public async Task<ProviderDto> Handle(GetProviderByIdQuery request, CancellationToken cancellationToken)
    {
        var provider = await _providers.GetByIdAsync(request.ProviderId, cancellationToken)
            ?? throw new EntityNotFoundException("Provider", request.ProviderId);
        return provider.ToDto();
    }
}
