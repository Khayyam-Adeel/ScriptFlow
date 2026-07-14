using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetPracticeByIdQueryHandler : IRequestHandler<GetPracticeByIdQuery, PracticeDto>
{
    private readonly IPracticeRepository _practices;

    public GetPracticeByIdQueryHandler(IPracticeRepository practices)
    {
        _practices = practices;
    }

    public async Task<PracticeDto> Handle(GetPracticeByIdQuery request, CancellationToken cancellationToken)
    {
        var practice = await _practices.GetByIdAsync(request.PracticeId, cancellationToken)
            ?? throw new EntityNotFoundException("Practice", request.PracticeId);
        return practice.ToDto();
    }
}
