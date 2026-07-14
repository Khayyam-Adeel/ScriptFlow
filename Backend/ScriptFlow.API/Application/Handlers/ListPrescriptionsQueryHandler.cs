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
    private readonly IPatientRepository _patients;
    private readonly IProviderRepository _providers;

    public ListPrescriptionsQueryHandler(
        IPrescriptionRepository prescriptions, IMedicineRepository medicines,
        IPatientRepository patients, IProviderRepository providers)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _patients = patients;
        _providers = providers;
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

        // Medicine/patient/provider names for the grid, each batched (never one request per
        // row) and run concurrently - each hits its own connection (SqlConnectionFactory opens
        // a fresh one per call), so there's no reason to pay for three round trips back to back.
        var medicineIds = prescriptions.SelectMany(p => p.Medications.Select(m => m.MedicineId)).Distinct();
        var patientIds = prescriptions.Select(p => p.PatientId).Distinct();
        var providerIds = prescriptions.Select(p => p.ProviderId).Distinct();

        var medicinesByIdTask = _medicines.GetManyAsync(medicineIds, cancellationToken);
        var patientsByIdTask = _patients.GetManyAsync(patientIds, cancellationToken);
        var providersByIdTask = _providers.GetManyAsync(providerIds, cancellationToken);
        await Task.WhenAll(medicinesByIdTask, patientsByIdTask, providersByIdTask);

        return prescriptions
            .Select(p => p.ToDto(medicinesByIdTask.Result, patientsByIdTask.Result, providersByIdTask.Result))
            .ToList();
    }
}
