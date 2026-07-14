using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.Events;
using Shared.Infrastructure.Correlation;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

public sealed class CreatePrescriptionCommandHandler : IRequestHandler<CreatePrescriptionCommand, PrescriptionDto>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IPatientRepository _patients;
    private readonly IProviderRepository _providers;
    private readonly IPracticeLocationRepository _practiceLocations;
    private readonly IMedicineRepository _medicines;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public CreatePrescriptionCommandHandler(
        IPrescriptionRepository prescriptions,
        IPatientRepository patients,
        IProviderRepository providers,
        IPracticeLocationRepository practiceLocations,
        IMedicineRepository medicines,
        IEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        _prescriptions = prescriptions;
        _patients = patients;
        _providers = providers;
        _practiceLocations = practiceLocations;
        _medicines = medicines;
        _eventPublisher = eventPublisher;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<PrescriptionDto> Handle(CreatePrescriptionCommand request, CancellationToken cancellationToken)
    {
        // FR-003: validate the patient (and the referenced provider/location/medicines) exist before creating anything.
        var patient = await _patients.GetByIdAsync(request.PatientId, cancellationToken)
            ?? throw new EntityNotFoundException("Patient", request.PatientId);

        var provider = await _providers.GetByIdAsync(request.ProviderId, cancellationToken)
            ?? throw new EntityNotFoundException("Provider", request.ProviderId);

        var practiceLocation = await _practiceLocations.GetByIdAsync(request.PracticeLocationId, cancellationToken)
            ?? throw new EntityNotFoundException("PracticeLocation", request.PracticeLocationId);

        var medicineIds = request.Medications.Select(m => m.MedicineId).Distinct().ToList();
        var medicinesById = await _medicines.GetManyAsync(medicineIds, cancellationToken);
        var missingMedicineIds = medicineIds.Where(id => !medicinesById.ContainsKey(id)).ToList();
        if (missingMedicineIds.Count > 0)
        {
            throw new EntityNotFoundException("Medicine", missingMedicineIds[0]);
        }

        var medications = request.Medications.Select(m => new PrescriptionMedication(
            Guid.NewGuid(), m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions,
            m.Route, m.Strength, m.IsPrn, m.Notes, m.Repeats));

        var prescription = new Prescription(
            Guid.NewGuid(), Scid.Generate(), patient.Id, provider.Id, practiceLocation.Id, medications);

        await _prescriptions.AddAsync(prescription, cancellationToken);

        await _eventPublisher.PublishAsync(new PrescriptionCreatedEvent
        {
            PrescriptionId = prescription.Id,
            Scid = prescription.Scid.Value,
            PatientId = prescription.PatientId,
            ProviderId = prescription.ProviderId,
            Status = prescription.Status,
            CorrelationId = _correlationIdAccessor.CorrelationId
        }, cancellationToken);

        return prescription.ToDto(medicinesById);
    }
}
