using Shared.contract.Contracts;

namespace Dispatch.Worker.Application.Interfaces;

public interface IPharmacyGatewayClient
{
    Task<PharmacyDispatchResponse> DispatchAsync(PharmacyDispatchRequest request, CancellationToken cancellationToken);
}
