using System.Data;
using Dapper;
using ScriptFlow.API.Application.Interfaces;
using ScriptFlow.API.Domain.Entities;
using ScriptFlow.API.Infrastructure.Database;

namespace ScriptFlow.API.Infrastructure.Persistence;

public sealed class SqlMedicineRepository : IMedicineRepository
{
    private readonly ISqlConnectionFactory _connectionFactory;

    public SqlMedicineRepository(ISqlConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<Medicine?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var row = await connection.QuerySingleOrDefaultAsync<MedicineRow>(new CommandDefinition(
            "Lookup.usp_Medicine_GetById",
            new { Id = id },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return row is null ? null : ToEntity(row);
    }

    public async Task<IReadOnlyDictionary<Guid, Medicine>> GetManyAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idTable = new DataTable();
        idTable.Columns.Add("Id", typeof(Guid));
        foreach (var id in ids.Distinct())
        {
            idTable.Rows.Add(id);
        }

        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<MedicineRow>(new CommandDefinition(
            "Lookup.usp_Medicine_GetByIds",
            new { Ids = idTable.AsTableValuedParameter("dbo.tvpGuidList") },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.Select(ToEntity).ToDictionary(m => m.Id, m => m);
    }

    public async Task<IReadOnlyCollection<Medicine>> ListAsync(string? search, CancellationToken cancellationToken = default)
    {
        using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var rows = await connection.QueryAsync<MedicineRow>(new CommandDefinition(
            "Lookup.usp_Medicine_List",
            new { Search = search },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        return rows.Select(ToEntity).ToList();
    }

    private static Medicine ToEntity(MedicineRow row) => new(row.Id, row.Name, row.Sctid, row.Form);

    private sealed record MedicineRow(Guid Id, string Name, string Sctid, string Form);
}
