using System.Net.Http.Json;
using Dispatch.Worker.Application.Interfaces;
using Shared.contract.Contracts;

namespace Dispatch.Worker.Infrastructure.Pharmacy;

// Talks to PharmacyGateway.mock over HTTP. The Polly retry policy that absorbs its
// simulated dropped connections is attached to this HttpClient at registration time (see
// DispatchWorkerServiceCollectionExtensions), not here - this class only makes one attempt
// and either returns a parsed response or lets the transport exception propagate.
public sealed class PharmacyGatewayHttpClient : IPharmacyGatewayClient
{
    private readonly HttpClient _httpClient;

    public PharmacyGatewayHttpClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PharmacyDispatchResponse> DispatchAsync(PharmacyDispatchRequest request, CancellationToken cancellationToken)
    {
        using var httpResponse = await _httpClient.PostAsJsonAsync("api/pharmacy/dispatch", request, cancellationToken);
        httpResponse.EnsureSuccessStatusCode();

        var response = await httpResponse.Content.ReadFromJsonAsync<PharmacyDispatchResponse>(cancellationToken: cancellationToken);
        return response ?? throw new InvalidOperationException("Pharmacy gateway returned an empty response body");
    }
}
