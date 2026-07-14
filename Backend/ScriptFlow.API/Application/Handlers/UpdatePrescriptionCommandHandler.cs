using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Application.Handlers;

public sealed class UpdatePrescriptionCommandHandler : IRequestHandler<UpdatePrescriptionCommand, PrescriptionDto>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;

    public UpdatePrescriptionCommandHandler(IPrescriptionRepository prescriptions, IMedicineRepository medicines)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
    }

    public async Task<PrescriptionDto> Handle(UpdatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", request.PrescriptionId);

        var medicineIds = request.Medications.Select(m => m.MedicineId).Distinct().ToList();
        var medicinesById = await _medicines.GetManyAsync(medicineIds, cancellationToken);
        var missingMedicineIds = medicineIds.Where(id => !medicinesById.ContainsKey(id)).ToList();
        if (missingMedicineIds.Count > 0)
        {
            throw new EntityNotFoundException("Medicine", missingMedicineIds[0]);
        }

        var medications = request.Medications.Select(m => new PrescriptionMedication(
            Guid.NewGuid(), m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions,
            m.Route, m.Strength, m.IsPrn, m.Notes));

        // Throws InvalidPrescriptionStateException (-> 409) if the prescription is no longer in Created status.
        prescription.UpdateMedications(medications);
        await _prescriptions.UpdateAsync(prescription, cancellationToken);

        return prescription.ToDto(medicinesById);
    }
}
