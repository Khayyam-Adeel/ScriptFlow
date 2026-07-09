using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class SearchPatientsQueryHandler : IRequestHandler<SearchPatientsQuery, IReadOnlyCollection<PatientDto>>
{
    private readonly IPatientRepository _patients;

    public SearchPatientsQueryHandler(IPatientRepository patients)
    {
        _patients = patients;
    }

    public async Task<IReadOnlyCollection<PatientDto>> Handle(SearchPatientsQuery request, CancellationToken cancellationToken)
    {
        var patients = await _patients.SearchAsync(request.Query, cancellationToken);
        return patients.Select(p => p.ToDto()).ToList();
    }
}
