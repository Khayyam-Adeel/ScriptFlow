using ScriptFlow.API.Domain.Exceptions;

namespace ScriptFlow.API.Domain.Entities;

/// <summary>One medication line item on a prescription, mapped to a <see cref="Medicine"/>.</summary>
public sealed class PrescriptionMedication
{
    public Guid Id { get; }
    public Guid MedicineId { get; }
    public string TakeValue { get; }
    public string Frequency { get; }
    public string Duration { get; }
    public int Quantity { get; }
    public string Directions { get; }

    public PrescriptionMedication(
        Guid id, Guid medicineId, string takeValue, string frequency, string duration, int quantity, string directions)
    {
        if (medicineId == Guid.Empty)
        {
            throw new DomainException("Medication must reference a medicine.");
        }

        if (string.IsNullOrWhiteSpace(takeValue))
        {
            throw new DomainException("Medication take value is required.");
        }

        if (string.IsNullOrWhiteSpace(frequency))
        {
            throw new DomainException("Medication frequency is required.");
        }

        if (string.IsNullOrWhiteSpace(duration))
        {
            throw new DomainException("Medication duration is required.");
        }

        if (quantity <= 0)
        {
            throw new DomainException("Medication quantity must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(directions))
        {
            throw new DomainException("Medication directions are required.");
        }

        Id = id;
        MedicineId = medicineId;
        TakeValue = takeValue;
        Frequency = frequency;
        Duration = duration;
        Quantity = quantity;
        Directions = directions;
    }
}
