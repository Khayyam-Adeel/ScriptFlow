using MediatR;
using ScriptFlow.API.Application.Commands;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Application.Mappings;
using ScriptFlow.API.Domain.Exceptions;
using ScriptFlow.API.Domain.ValueObjects;
using Shared.Events;
using Shared.Infrastructure.Correlation;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Application.Handlers;

public sealed class RepeatPrescriptionCommandHandler : IRequestHandler<RepeatPrescriptionCommand, PrescriptionDto>
{
    private readonly IPrescriptionRepository _prescriptions;
    private readonly IMedicineRepository _medicines;
    private readonly IEventPublisher _eventPublisher;
    private readonly ICorrelationIdAccessor _correlationIdAccessor;

    public RepeatPrescriptionCommandHandler(
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

    public async Task<PrescriptionDto> Handle(RepeatPrescriptionCommand request, CancellationToken cancellationToken)
    {
        var original = await _prescriptions.GetByIdAsync(request.PrescriptionId, cancellationToken)
            ?? throw new EntityNotFoundException("Prescription", request.PrescriptionId);

        // Throws InvalidPrescriptionStateException (-> 409) unless the original is Signed/Dispatched/Acknowledged.
        var repeated = original.Repeat(Guid.NewGuid(), Scid.Generate());
        await _prescriptions.AddAsync(repeated, cancellationToken);

        var medicinesById = await _medicines.GetManyAsync(repeated.Medications.Select(m => m.MedicineId), cancellationToken);

        await _eventPublisher.PublishAsync(new PrescriptionRepeatedEvent
        {
            PrescriptionId = repeated.Id,
            Scid = repeated.Scid.Value,
            RepeatOfPrescriptionId = original.Id,
            PatientId = repeated.PatientId,
            ProviderId = repeated.ProviderId,
            Status = repeated.Status,
            CorrelationId = _correlationIdAccessor.CorrelationId
        }, cancellationToken);

        return repeated.ToDto(medicinesById);
    }
}
