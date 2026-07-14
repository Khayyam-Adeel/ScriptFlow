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

    /// <summary>Optional administration route (e.g. "Oral", "Topical", "IV"). Free text so the
    /// prescriber isn't boxed into a fixed list; null when not specified.</summary>
    public string? Route { get; }

    /// <summary>Optional dose strength (e.g. "500 mg", "10 mg/mL"); null when not specified.</summary>
    public string? Strength { get; }

    /// <summary>PRN = "pro re nata": taken as needed rather than on a fixed schedule.</summary>
    public bool IsPrn { get; }

    /// <summary>Optional free-text note for the pharmacist/patient; null when not specified.</summary>
    public string? Notes { get; }

    public PrescriptionMedication(
        Guid id, Guid medicineId, string takeValue, string frequency, string duration, int quantity, string directions,
        string? route = null, string? strength = null, bool isPrn = false, string? notes = null)
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
        // Optional fields: normalise blank/whitespace to null so "not specified" is one value.
        Route = string.IsNullOrWhiteSpace(route) ? null : route.Trim();
        Strength = string.IsNullOrWhiteSpace(strength) ? null : strength.Trim();
        IsPrn = isPrn;
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
    }
}
