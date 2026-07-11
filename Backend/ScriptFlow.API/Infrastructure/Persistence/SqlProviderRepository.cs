using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Infrastructure.Database;
using Shared.contract.Enums;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class SqlProviderRepository : IProviderRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;
    private readonly ICurrentUserAccessor _currentUser;

    public SqlProviderRepository(ISqlConnectionFactory connectionFactory, ICurrentUserAccessor currentUser)
    {
        _connectionFactory = connectionFactory;
        _currentUser = currentUser;
    }

    public async Task<Provider?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<ProviderRow>(new CommandDefinition(
            "Profile.usp_Provider_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return row is null ? null : ToEntity(row);
    }

    public async Task AddAsync(Provider provider, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "Profile.usp_Provider_Create",
            new
            {
                provider.Id,
                provider.FirstName,
                provider.LastName,
                Type = (byte)provider.Type,
                provider.NzmcNo,
                provider.PracticeLocationId,
                InsertedBy = _currentUser.UserId
            },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task<IReadOnlyCollection<Provider>> ListAsync(Guid? practiceLocationId, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<ProviderRow>(new CommandDefinition(
            "Profile.usp_Provider_List",
            new { PracticeLocationId = practiceLocationId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.Select(ToEntity).ToList();
    }

    private static Provider ToEntity(ProviderRow row) =>
        new(row.Id, row.FirstName, row.LastName, (ProviderType)row.Type, row.NzmcNo, row.PracticeLocationId);

    private sealed record ProviderRow(Guid Id, string FirstName, string LastName, byte Type, string NzmcNo, Guid PracticeLocationId);
}
