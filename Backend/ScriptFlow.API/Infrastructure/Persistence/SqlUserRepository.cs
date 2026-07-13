using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Infrastructure.Database;
using Shared.contract.Enums;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class SqlUserRepository : IUserRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlUserRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<UserRow>(new CommandDefinition(
            "Profile.usp_User_GetByEmail",
            new { Email = email },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return row is null ? null : new User(row.Id, row.Email, row.PasswordHash, (UserRole)row.Role);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "Profile.usp_User_Create",
            new { user.Id, user.Email, user.PasswordHash, Role = (byte)user.Role },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    private sealed record UserRow(Guid Id, string Email, string PasswordHash, byte Role);
}
