using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.ValueObjects;
using ScriptFlow.API.Infrastructure.Database;

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

    private static Patient ToEntity(PatientRow row) =>
        new(row.Id, row.FirstName, row.LastName, row.Address, new Nhi(row.Nhi));

    private sealed record PatientRow(Guid Id, string FirstName, string LastName, string Address, string Nhi);
}
