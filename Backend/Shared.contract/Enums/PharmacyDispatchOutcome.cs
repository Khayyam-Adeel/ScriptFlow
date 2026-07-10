namespace Shared.contract.Enums;

// The two possible business answers a pharmacy can give back for a dispatch attempt.
// A dropped/failed call has no value here - it never reaches a parsed response.
public enum PharmacyDispatchOutcome
{
    Acknowledged = 0,
    Rejected = 1
}
