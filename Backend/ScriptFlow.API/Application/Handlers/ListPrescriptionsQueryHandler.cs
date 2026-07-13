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
        // CreatedTo comes from the frontend as an inclusive calendar day (the last day the user
        // wants included); convert to an exclusive upper bound here so the repository/proc can
        // use a simple "< CreatedToExclusive" range check.
        var createdToExclusive = request.CreatedTo?.Date.AddDays(1);

        var prescriptions = await _prescriptions.ListAsync(
            request.PatientId, request.ProviderId, request.Status, request.ScidPrefix,
            request.CreatedFrom, createdToExclusive, cancellationToken);
        var medicineIds = prescriptions.SelectMany(p => p.Medications.Select(m => m.MedicineId)).Distinct();
        var medicinesById = await _medicines.GetManyAsync(medicineIds, cancellationToken);

        return prescriptions.Select(p => p.ToDto(medicinesById)).ToList();
    }
}
