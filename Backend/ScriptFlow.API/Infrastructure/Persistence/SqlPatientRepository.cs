using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.ValueObjects;
using ScriptFlow.API.Infrastructure.Database;
using Shared.contract.Enums;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class SqlPatientRepository : IPatientRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ICurrentUserAccessor _currentUser;

    public SqlPatientRepository(ISqlConnectionFactory connectionFactory, ICurrentUserAccessor currentUser)
    {
        _connectionFactory = connectionFactory;
        _currentUser = currentUser;
    }

    public async Task<Patient?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PatientRow>(new CommandDefinition(
            "Profile.usp_Patient_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return row is null ? null : ToEntity(row);
    }

    public async Task AddAsync(Patient patient, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "Profile.usp_Patient_Create",
            new
            {
                patient.Id,
                patient.FirstName,
                patient.LastName,
                patient.Address,
                Nhi = patient.Nhi.Value,
                DateOfBirth = patient.DateOfBirth.ToDateTime(TimeOnly.MinValue),
                Gender = (byte)patient.Gender,
                patient.PhoneNumber,
                patient.Email,
                InsertedBy = _currentUser.UserId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<Patient>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PatientRow>(new CommandDefinition(
            "Profile.usp_Patient_Search",
            new { Query = query },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.Select(ToEntity).ToList();
    }

    public async Task<IReadOnlyDictionary<Guid, Patient>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idTable = new DataTable();
        idTable.Columns.Add("Id", typeof(Guid));
        foreach (var id in ids.Distinct())
        {
            idTable.Rows.Add(id);
        }

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PatientRow>(new CommandDefinition(
            "Profile.usp_Patient_GetByIds",
            new { Ids = idTable.AsTableValuedParameter("dbo.tvpGuidList") },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.Select(ToEntity).ToDictionary(p => p.Id, p => p);
    }

    private static Patient ToEntity(PatientRow row) =>
        new(row.Id, row.FirstName, row.LastName, row.Address, new Nhi(row.Nhi),
            DateOnly.FromDateTime(row.DateOfBirth), (Gender)row.Gender, row.PhoneNumber, row.Email);

    private sealed record PatientRow(
        Guid Id, string FirstName, string LastName, string Address, string Nhi,
        DateTime DateOfBirth, byte Gender, string PhoneNumber, string Email);
}
