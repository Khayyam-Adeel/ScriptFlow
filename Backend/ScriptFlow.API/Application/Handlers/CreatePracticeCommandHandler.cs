using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Entities;

namespace ScriptFlow.API.Application.Handlers;

public sealed class CreatePracticeCommandHandler : IRequestHandler<CreatePracticeCommand, PracticeDto>
{
    private readonly IPracticeRepository _practices;

    public CreatePracticeCommandHandler(IPracticeRepository practices)
    {
        _practices = practices;
    }

    public async Task<PracticeDto> Handle(CreatePracticeCommand request, CancellationToken cancellationToken)
    {
        var practice = new Practice(Guid.NewGuid(), request.Name);
        await _practices.AddAsync(practice, cancellationToken);
        return practice.ToDto();
    }
}
