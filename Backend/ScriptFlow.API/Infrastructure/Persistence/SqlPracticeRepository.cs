using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Infrastructure.Database;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class SqlPracticeRepository : IPracticeRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ICurrentUserAccessor _currentUser;

    public SqlPracticeRepository(ISqlConnectionFactory connectionFactory, ICurrentUserAccessor currentUser)
    {
        _connectionFactory = connectionFactory;
        _currentUser = currentUser;
    }

    public async Task<Practice?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<PracticeRow>(new CommandDefinition(
            "Admin.usp_Practice_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return row is null ? null : new Practice(row.Id, row.Name);
    }

    public async Task AddAsync(Practice practice, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "Admin.usp_Practice_Create",
            new { practice.Id, practice.Name, InsertedBy = _currentUser.UserId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<Practice>> ListAsync(CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<PracticeRow>(new CommandDefinition(
            "Admin.usp_Practice_List",
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.Select(row => new Practice(row.Id, row.Name)).ToList();
    }

    private sealed record PracticeRow(Guid Id, string Name);
}
