using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class ListPracticesQueryHandler : IRequestHandler<ListPracticesQuery, IReadOnlyCollection<PracticeDto>>
{
    private readonly IPracticeRepository _practices;

    public ListPracticesQueryHandler(IPracticeRepository practices)
    {
        _practices = practices;
    }

    public async Task<IReadOnlyCollection<PracticeDto>> Handle(ListPracticesQuery request, CancellationToken cancellationToken)
    {
        var practices = await _practices.ListAsync(cancellationToken);
        return practices.Select(p => p.ToDto()).ToList();
    }
}
