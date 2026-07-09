using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetPatientByIdQueryHandler : IRequestHandler<GetPatientByIdQuery, PatientDto>
{
    private readonly IPatientRepository _patients;

    public GetPatientByIdQueryHandler(IPatientRepository patients)
    {
        _patients = patients;
    }

    public async Task<PatientDto> Handle(GetPatientByIdQuery request, CancellationToken cancellationToken)
    {
        var patient = await _patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new EntityNotFoundException("Patient", request.PatientId);
        return patient.ToDto();
    }
}
