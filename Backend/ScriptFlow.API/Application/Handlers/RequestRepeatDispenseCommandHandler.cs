using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Exceptions;
using Shared.Events;
using Shared.Infrastructure.Correlation;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

/// <summary>
/// Re-enters an already-signed, pharmacy-acknowledged prescription into the same
/// Sign -&gt; Dispatch -&gt; Acknowledge/Reject pipeline for a repeat dispense, instead of
/// creating a new prescription. See Prescription.RequestRepeatDispense for the eligibility
/// rule (all medications must still have a repeat remaining).
/// </summary>
public sealed class RequestRepeatDispenseCommandHandler : IRequestHandler<RequestRepeatDispenseCommand, PrescriptionDto>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public RequestRepeatDispenseCommandHandler(
        IPrescriptionRepository prescriptions,
        IMedicineRepository medicines,
        IEventPublisher eventPublisher,
        ICorrelationIdAccessor correlationIdAccessor)
    {
        _prescriptions = prescriptions;
        _medicines = medicines;
        _eventPublisher = eventPublisher;
        _correlationIdAccessor = correlationIdAccessor;
    }

    public async Task<PrescriptionDto> Handle(RequestRepeatDispenseCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", request.PrescriptionId);

        // Throws InvalidPrescriptionStateException (-> 409) unless Acknowledged with repeats remaining.
        prescription.RequestRepeatDispense();
        await _prescriptions.UpdateAsync(prescription, cancellationToken);

        var medicinesById = await _medicines.GetManyAsync(prescription.Medications.Select(m => m.MedicineId), cancellationToken);

        await _eventPublisher.PublishAsync(new PrescriptionSignedEvent
        {
            PrescriptionId = prescription.Id,
            Scid = prescription.Scid.Value,
            ProviderId = prescription.ProviderId,
            SignedAtUtc = prescription.SignedAtUtc!.Value,
            Status = prescription.Status,
            CorrelationId = _correlationIdAccessor.CorrelationId,
            IsRepeatDispense = true
        }, cancellationToken);

        return prescription.ToDto(medicinesById);
    }
}
