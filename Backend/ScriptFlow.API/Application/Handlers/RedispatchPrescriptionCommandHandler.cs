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
/// Operator-triggered retry for a prescription stuck at Dispatched - e.g. Dispatch.Worker's
/// pharmacy call exhausted its Polly retries and the message dead-lettered before ever
/// publishing an Acknowledged/Rejected outcome (see PrescriptionSignedEventHandler). Re-enters
/// the same Sign -> Dispatch -> Acknowledge/Reject pipeline a fresh signature would, the same way
/// RequestRepeatDispenseCommandHandler does for a repeat dispense.
/// </summary>
public sealed class RedispatchPrescriptionCommandHandler : IRequestHandler<RedispatchPrescriptionCommand, PrescriptionDto>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public RedispatchPrescriptionCommandHandler(
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

    public async Task<PrescriptionDto> Handle(RedispatchPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var prescription = await _prescriptions.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", request.PrescriptionId);

        // Throws InvalidPrescriptionStateException (-> 409) unless currently Dispatched.
        prescription.RequestRedispatch();
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
            IsRepeatDispense = false
        }, cancellationToken);

        return prescription.ToDto(medicinesById);
    }
}
