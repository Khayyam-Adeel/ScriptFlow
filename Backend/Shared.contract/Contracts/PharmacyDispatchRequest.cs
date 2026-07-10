namespace Shared.contract.Contracts;

// What Dispatch.Worker sends to PharmacyGateway.mock to ask it to dispatch a prescription.
public sealed class PharmacyDispatchRequest
{
    public required Guid PrescriptionId { get; init; }
    public required string Scid { get; init; }

    // Carried through so the mock gateway's own logs can be correlated back to the
    // request that triggered them, even though it has no business need for the value.
    public required string CorrelationId { get; init; }
}
