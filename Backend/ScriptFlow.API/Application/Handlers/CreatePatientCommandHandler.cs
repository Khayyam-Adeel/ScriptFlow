using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Application.Handlers;

public sealed class CreatePatientCommandHandler : IRequestHandler<CreatePatientCommand, PatientDto>
{
    private readonly IPatientRepository _patients;

    public CreatePatientCommandHandler(IPatientRepository patients)
    {
        _patients = patients;
    }

    public async Task<PatientDto> Handle(CreatePatientCommand request, CancellationToken cancellationToken)
    {
        var patient = new Patient(
            Guid.NewGuid(), request.FirstName, request.LastName, request.Address, new Nhi(request.Nhi),
            request.DateOfBirth, request.Gender, request.PhoneNumber, request.Email);
        await _patients.AddAsync(patient, cancellationToken);
        return patient.ToDto();
    }
}
