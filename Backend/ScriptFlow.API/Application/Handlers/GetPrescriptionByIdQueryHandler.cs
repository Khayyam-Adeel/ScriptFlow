using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class GetPrescriptionByIdQueryHandler : IRequestHandler<GetPrescriptionByIdQuery, PrescriptionDto>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;

    public GetPrescriptionByIdQueryHandler(IPrescriptionRepository prescriptions, IMedicineRepository medicines)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
    }

    public async Task<PrescriptionDto> Handle(GetPrescriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", request.PrescriptionId);

        var medicinesById = await _medicines.GetManyAsync(prescription.Medications.Select(m => m.MedicineId), cancellationToken);
        return prescription.ToDto(medicinesById);
    }
}
