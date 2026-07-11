namespace ScriptFlow.API.Application.Interfaces;

/// <summary>Resolves the authenticated caller's user id from the current HTTP request, for the
/// InsertedBy/UpdatedBy audit columns SQL-backed repositories write on every insert/update. Only
/// valid to read while handling an authenticated (JWT-bearer) request.</summary>
public interface ICurrentUserAccessor
{
    Guid UserId { get; }

    /// <summary>Same as <see cref="UserId"/> but returns null instead of throwing when there is no
    /// HTTP request to resolve a user from - e.g. a RabbitMQ consumer's BackgroundService callback
    /// updating a prescription's status in response to a pharmacy outcome, not a user action.
    /// UpdatedBy is nullable in the schema specifically to allow these system-driven writes.</summary>
    Guid? UserIdOrNull { get; }
}
