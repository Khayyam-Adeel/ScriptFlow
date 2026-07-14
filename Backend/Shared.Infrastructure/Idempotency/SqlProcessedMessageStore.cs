using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Shared.Infrastructure.Idempotency;

/// <summary>
/// Durable idempotency ledger backed by dbo.ProcessedMessages - shared by every event-consuming
/// service that needs real dedup (ScriptFlow.API's prescription lifecycle handlers,
/// Dispatch.Worker's signed handler), so a redelivered message is still caught after a crash or
/// restart, unlike the in-memory store this replaces (that only deduped within one process's
/// lifetime). Notification.Service deliberately does not use this - a duplicate SignalR
/// broadcast is harmless, so it isn't a real idempotency case.
/// </summary>
public sealed class SqlProcessedMessageStore : IProcessedMessageStore
{
    // Shared.Infrastructure can't reference ScriptFlow.API.Domain.Entities.SystemUser (wrong
    // dependency direction) - same well-known system user id, duplicated here as a raw constant;
    // single source of truth is the seeded Profile.tblUsers row itself (see 01_SeedSystemUser.sql).
    private static readonly Guid SystemUserId = Guid.Parse("00000000-0000-0000-0000-0000000000AA");

    private readonly string _connectionString;

    public SqlProcessedMessageStore(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("ScriptFlowDb")
            ?? throw new InvalidOperationException("Missing ConnectionStrings:ScriptFlowDb configuration value.");
    }

    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return await connection.QuerySingleAsync<bool>(new CommandDefinition(
            "dbo.usp_ProcessedMessage_IsProcessed",
            new { EventId = eventId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }

    public async Task MarkProcessedAsync(Guid eventId, string eventType, Guid prescriptionId, CancellationToken cancellationToken)
    {
        using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            "dbo.usp_ProcessedMessage_MarkProcessed",
            new { EventId = eventId, EventType = eventType, PrescriptionId = prescriptionId, InsertedBy = SystemUserId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));
    }
}
