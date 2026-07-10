using System.Data;

namespace ScriptFlow.API.Infrastructure.Database;

/// <summary>Hands out an open ADO.NET connection for repositories to run Dapper calls against.</summary>
public interface ISqlConnectionFactory
{
    Task<IDbConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}
