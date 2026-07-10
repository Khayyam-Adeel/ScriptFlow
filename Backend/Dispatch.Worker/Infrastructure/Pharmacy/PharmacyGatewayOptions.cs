namespace Dispatch.Worker.Infrastructure.Pharmacy;

public sealed class PharmacyGatewayOptions
{
    public const string SectionName = "PharmacyGateway";

    public string BaseUrl { get; set; } = "http://localhost:5287";
    public int TimeoutSeconds { get; set; } = 10;
}
