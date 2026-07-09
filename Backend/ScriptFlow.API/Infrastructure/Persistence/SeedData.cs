using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.ValueObjects;

namespace ScriptFlow.API.Infrastructure.Persistence;

/// <summary>
/// Fixed, well-known reference data for local development/testing (no real DB in this pass).
/// The GUIDs are hardcoded and readable so they can be copy-pasted straight into Swagger.
/// </summary>
internal static class SeedData
{
    public static readonly Practice Practice = new(
        Guid.Parse("00000000-0000-0000-0000-000000000001"),
        "Ponsonby Medical Centre");

    public static readonly PracticeLocation PracticeLocation = new(
        Guid.Parse("00000000-0000-0000-0000-000000000002"),
        Practice.Id,
        "Ponsonby Medical Centre - Main",
        new HpiNumber("FZZ99", "B"));

    public static readonly IReadOnlyList<Medicine> Medicines = new List<Medicine>
    {
        new(Guid.Parse("00000000-0000-0000-0000-000000000101"), "Amoxicillin 250mg Capsules", "27658006", "Capsule"),
        new(Guid.Parse("00000000-0000-0000-0000-000000000102"), "Paracetamol 500mg Tablets", "387517004", "Tablet"),
        new(Guid.Parse("00000000-0000-0000-0000-000000000103"), "Ibuprofen 400mg Tablets", "318272006", "Tablet"),
        new(Guid.Parse("00000000-0000-0000-0000-000000000104"), "Amoxicillin 250mg/5mL Oral Suspension", "322236009", "Oral Liquid"),
        new(Guid.Parse("00000000-0000-0000-0000-000000000105"), "Salbutamol 100micrograms/dose Inhaler", "317942008", "Inhaler")
    };
}
