using MediatR;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Application.Queries;

namespace ScriptFlow.API.Application.Handlers;

public sealed class ListPrescriptionsQueryHandler : IRequestHandler<ListPrescriptionsQuery, IReadOnlyCollection<PrescriptionDto>>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;

    public ListPrescriptionsQueryHandler(IPrescriptionRepository prescriptions, IMedicineRepository medicines)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
    }

    public async Task<IReadOnlyCollection<PrescriptionDto>> Handle(ListPrescriptionsQuery request, CancellationToken cancellationToken)
    {
        var prescriptions = await _prescriptions.ListAsync(request.PatientId, request.Status, cancellationToken);
        var medicineIds = prescriptions.SelectMany(p => p.Medications.Select(m => m.MedicineId)).Distinct();
        var medicinesById = await _medicines.GetManyAsync(medicineIds, cancellationToken);

        return prescriptions.Select(p => p.ToDto(medicinesById)).ToList();
    }
}
