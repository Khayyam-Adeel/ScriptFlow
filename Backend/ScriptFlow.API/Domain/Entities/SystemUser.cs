namespace ScriptFlow.API.Domain.Entities;

/// <summary>
/// The well-known "system" row in Profile.tblUsers that SPEC/DatabaseSpec.md's InsertedBy/UpdatedBy
/// convention refers to ("system/seed rows may use a well-known system user id"). Used to attribute
/// writes that happen outside any HTTP request - e.g. a RabbitMQ consumer moving a prescription to
/// Acknowledged/Rejected in response to a pharmacy outcome, where there is no interactive user.
/// </summary>
public static class SystemUser
{
    public static readonly Guid Id = Guid.Parse("00000000-0000-0000-0000-0000000000AA");
}
