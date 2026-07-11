using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class ListProvidersQueryHandler : IRequestHandler<ListProvidersQuery, IReadOnlyCollection<ProviderDto>>
{
    private readonly IProviderRepository _providers;

    public ListProvidersQueryHandler(IProviderRepository providers)
    {
        _providers = providers;
    }

    public async Task<IReadOnlyCollection<ProviderDto>> Handle(ListProvidersQuery request, CancellationToken cancellationToken)
    {
        var providers = await _providers.ListAsync(request.PracticeLocationId, cancellationToken);
        return providers.Select(p => p.ToDto()).ToList();
    }
}
