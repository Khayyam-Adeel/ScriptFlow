using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.ValueObjects;
using ScriptFlow.API.Infrastructure.Database;
using Shared.contract.Enums;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class SqlPrescriptionRepository : IPrescriptionRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ICurrentUserAccessor _currentUser;

    public SqlPrescriptionRepository(ISqlConnectionFactory connectionFactory, ICurrentUserAccessor currentUser)
    {
        _connectionFactory = connectionFactory;
        _currentUser = currentUser;
    }

    public async Task<Prescription?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            "Prescription.usp_Prescription_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        var header = await results.ReadSingleOrDefaultAsync<PrescriptionHeaderRow>();
        if (header is null)
        {
            return null;
        }

        var medicationRows = await results.ReadAsync<PrescriptionMedicationRow>();
        return ToEntity(header, medicationRows);
    }

    public async Task AddAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "Prescription.usp_Prescription_Create",
            new
            {
                prescription.Id,
                Scid = prescription.Scid.Value,
                prescription.PatientId,
                prescription.ProviderId,
                prescription.PracticeLocationId,
                prescription.RepeatOfPrescriptionId,
                Medications = BuildMedicationsTable(prescription.Medications).AsTableValuedParameter("dbo.tvpMedicationLine"),
                InsertedBy = _currentUser.UserId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task UpdateAsync(Prescription prescription, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "Prescription.usp_Prescription_Update",
            new
            {
                prescription.Id,
                Status = (byte)prescription.Status,
                prescription.SignedAtUtc,
                Medications = BuildMedicationsTable(prescription.Medications).AsTableValuedParameter("dbo.tvpMedicationLine"),
                // Acknowledge/Reject are driven by a RabbitMQ consumer reacting to a pharmacy
                // outcome, not an HTTP request, so there is no interactive user to attribute the
                // write to. This proc re-inserts every PrescriptionMedications row on every call
                // (see the proc's own header comment) and that table's InsertedBy is NOT NULL, so
                // falling back to null here (unlike Prescriptions.UpdatedBy, which is nullable)
                // would fail the insert - use the well-known system user instead.
                UpdatedBy = _currentUser.UserIdOrNull ?? SystemUser.Id
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<Prescription>> ListAsync(
        Guid? patientId, PrescriptionStatus? status, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            "Prescription.usp_Prescription_List",
            new { PatientId = patientId, Status = (byte?)status },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        var headers = (await results.ReadAsync<PrescriptionHeaderRow>()).ToList();
        var medicationsByPrescriptionId = (await results.ReadAsync<PrescriptionMedicationListRow>())
            .ToLookup(m => m.PrescriptionId);

        return headers
            .Select(header => ToEntity(
                header,
                medicationsByPrescriptionId[header.Id].Select(m => new PrescriptionMedicationRow(
                    m.Id, m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions))))
            .ToList();
    }

    private static Prescription ToEntity(PrescriptionHeaderRow header, IEnumerable<PrescriptionMedicationRow> medicationRows)
    {
        var medications = medicationRows.Select(m =>
            new PrescriptionMedication(m.Id, m.MedicineId, m.TakeValue, m.Frequency, m.Duration, m.Quantity, m.Directions));

        return Prescription.Rehydrate(
            header.Id,
            new Scid(header.Scid),
            header.PatientId,
            header.ProviderId,
            header.PracticeLocationId,
            header.RepeatOfPrescriptionId,
            (PrescriptionStatus)header.Status,
            header.CreatedAtUtc,
            header.SignedAtUtc,
            medications);
    }

    private static DataTable BuildMedicationsTable(IEnumerable<PrescriptionMedication> medications)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(Guid));
        table.Columns.Add("MedicineId", typeof(Guid));
        table.Columns.Add("TakeValue", typeof(string));
        table.Columns.Add("Frequency", typeof(string));
        table.Columns.Add("Duration", typeof(string));
        table.Columns.Add("Quantity", typeof(int));
        table.Columns.Add("Directions", typeof(string));

        foreach (var medication in medications)
        {
            table.Rows.Add(
                medication.Id, medication.MedicineId, medication.TakeValue,
                medication.Frequency, medication.Duration, medication.Quantity, medication.Directions);
        }

        return table;
    }

    private sealed record PrescriptionHeaderRow(
        Guid Id, string Scid, Guid PatientId, Guid ProviderId, Guid PracticeLocationId,
        byte Status, Guid? RepeatOfPrescriptionId, DateTime CreatedAtUtc, DateTime? SignedAtUtc);

    private sealed record PrescriptionMedicationRow(
        Guid Id, Guid MedicineId, string TakeValue, string Frequency, string Duration, int Quantity, string Directions);

    private sealed record PrescriptionMedicationListRow(
        Guid Id, Guid MedicineId, string TakeValue, string Frequency, string Duration, int Quantity, string Directions,
        Guid PrescriptionId);
}
