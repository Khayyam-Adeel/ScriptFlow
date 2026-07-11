using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Domain.ValueObjects;
using ScriptFlow.API.Infrastructure.Database;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class SqlPracticeLocationRepository : IPracticeLocationRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ICurrentUserAccessor _currentUser;

    public SqlPracticeLocationRepository(ISqlConnectionFactory connectionFactory, ICurrentUserAccessor currentUser)
    {
        _connectionFactory = connectionFactory;
        _currentUser = currentUser;
    }

    public async Task<PracticeLocation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PracticeLocationRow>(new CommandDefinition(
            "Admin.usp_PracticeLocation_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return row is null ? null : ToEntity(row);
    }

    public async Task AddAsync(PracticeLocation practiceLocation, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "Admin.usp_PracticeLocation_Create",
            new
            {
                practiceLocation.Id,
                practiceLocation.PracticeId,
                practiceLocation.Name,
                HpiNo = practiceLocation.HpiNumber.HpiNo,
                HpiExtension = practiceLocation.HpiNumber.HpiExtension,
                InsertedBy = _currentUser.UserId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<PracticeLocation>> ListAsync(Guid? practiceId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PracticeLocationRow>(new CommandDefinition(
            "Admin.usp_PracticeLocation_List",
            new { PracticeId = practiceId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.Select(ToEntity).ToList();
    }

    private static PracticeLocation ToEntity(PracticeLocationRow row) =>
        new(row.Id, row.PracticeId, row.Name, new HpiNumber(row.HpiNo, row.HpiExtension));

    private sealed record PracticeLocationRow(Guid Id, Guid PracticeId, string Name, string HpiNo, string HpiExtension);
}
