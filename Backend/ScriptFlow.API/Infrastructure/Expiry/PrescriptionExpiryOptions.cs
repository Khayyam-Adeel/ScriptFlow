namespace ScriptFlow.API.Infrastructure.Expiry;

public sealed class PrescriptionExpiryOptions
{
    public const string SectionName = "PrescriptionExpiry";

    /// <summary>A Created/Signed/Dispatched prescription older than this (by CreatedAtUtc)
    /// is swept up as Expired.</summary>
    public int StaleAfterHours { get; set; } = 72;

    /// <summary>How often the sweep runs.</summary>
    public int IntervalMinutes { get; set; } = 60;
}
