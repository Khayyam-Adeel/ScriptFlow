using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using ScriptFlow.API.Application.DTOs;
using ScriptFlow.API.Infrastructure.Database;
using Shared.contract.Enums;
using Shared.Infrastructure.Messaging;

namespace ScriptFlow.API.Tests.Integration;

/// <summary>
/// Exercises the primary prescription workflow (SPEC/MainSpec.md) end-to-end through the
/// real ASP.NET Core pipeline - auth, validation, MediatR, the domain state machine - against
/// real infrastructure: the local SQL Server (dbserver-local/ScriptFlow_DEV) and RabbitMQ this
/// project already runs against day to day. Deliberately not Testcontainers/in-memory fakes:
/// the real stored procedures are gitignored (database-side artifacts, not versioned here), so
/// there is nothing for a hermetic container to run against. Skips (not fails) when that
/// infrastructure isn't reachable - e.g. in CI, which has no SQL Server/RabbitMQ configured.
/// </summary>
public sealed class PrimaryWorkflowIntegrationTests : IClassFixture<ScriptFlowApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly ScriptFlowApiFactory _factory;

    public PrimaryWorkflowIntegrationTests(ScriptFlowApiFactory factory)
    {
        _factory = factory;
    }

    [SkippableFact]
    public async Task CreateAndSignPrescription_PublishesRealSignedEventOverRabbitMq()
    {
        using var scope = _factory.Services.CreateScope();
        await SkipUnlessInfrastructureIsReachableAsync(scope.ServiceProvider);

        using var client = _factory.CreateClient();
        var runId = Guid.NewGuid().ToString("N")[..8];

        // 1. Register a real user, get a real JWT from a real DB-backed row.
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            email = $"integration-test-{runId}@example.com",
            password = "P@ssw0rd123!"
        });
        registerResponse.EnsureSuccessStatusCode();
        var auth = await registerResponse.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // 2. Reuse existing seeded master data (performance chapter already left plenty).
        var practiceLocation = (await client.GetFromJsonAsync<List<PracticeLocationDto>>(
            "/api/practice-locations", JsonOptions))!.First();
        var medicine = (await client.GetFromJsonAsync<List<MedicineDto>>(
            "/api/medicines", JsonOptions))!.First();

        // 3. Real provider + patient rows via the real stored procedures.
        var providerResponse = await client.PostAsJsonAsync("/api/providers", new
        {
            firstName = "Integration",
            lastName = "Tester",
            type = ProviderType.Doctor,
            nzmcNo = $"IT{runId}",
            practiceLocationId = practiceLocation.Id
        }, JsonOptions);
        providerResponse.EnsureSuccessStatusCode();
        var provider = await providerResponse.Content.ReadFromJsonAsync<ProviderDto>(JsonOptions);

        // NHI must be exactly 3 letters + 4 digits - derive both deterministically from a
        // hash of runId rather than slicing runId's hex characters directly (hex includes
        // a-f, which would fail the "4 digits" part of the format).
        var nhiSeed = Math.Abs(runId.GetHashCode());
        var nhi = $"{(char)('A' + nhiSeed % 26)}{(char)('A' + nhiSeed / 26 % 26)}{(char)('A' + nhiSeed / 676 % 26)}{nhiSeed % 10000:D4}";
        var patientResponse = await client.PostAsJsonAsync("/api/patients", new
        {
            firstName = "Integration",
            lastName = "Patient",
            address = "1 Test Street",
            nhi
        });
        patientResponse.EnsureSuccessStatusCode();
        var patient = await patientResponse.Content.ReadFromJsonAsync<PatientDto>(JsonOptions);

        // 4. Create the prescription - starts life as Created.
        var createResponse = await client.PostAsJsonAsync("/api/prescriptions", new
        {
            patientId = patient!.Id,
            providerId = provider!.Id,
            practiceLocationId = practiceLocation.Id,
            medications = new[]
            {
                new
                {
                    medicineId = medicine.Id,
                    takeValue = "1 tablet",
                    frequency = "Twice daily",
                    duration = "7 days",
                    quantity = 14,
                    directions = "Take with food"
                }
            }
        }, JsonOptions);
        createResponse.EnsureSuccessStatusCode();
        var prescription = await createResponse.Content.ReadFromJsonAsync<PrescriptionDto>(JsonOptions);
        Assert.Equal(PrescriptionStatus.Created, prescription!.Status);

        // 5. Start listening for the real event BEFORE signing, so there's no race between
        // publish and subscribe.
        var rabbitMqOptions = scope.ServiceProvider.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
        using var connection = new ConnectionFactory
        {
            HostName = rabbitMqOptions.HostName,
            Port = rabbitMqOptions.Port,
            UserName = rabbitMqOptions.UserName,
            Password = rabbitMqOptions.Password
        }.CreateConnection();
        using var channel = connection.CreateModel();
        channel.ExchangeDeclare(rabbitMqOptions.ExchangeName, ExchangeType.Topic, durable: true);
        var queueName = channel.QueueDeclare(exclusive: true).QueueName;
        channel.QueueBind(queueName, rabbitMqOptions.ExchangeName, routingKey: nameof(Shared.Events.PrescriptionSignedEvent));

        // 6. Sign it - the primary workflow's key transition (FR-002): Created -> Signed,
        // publishing PrescriptionSignedEvent.
        var signResponse = await client.PostAsync($"/api/prescriptions/{prescription.Id}/sign", content: null);
        signResponse.EnsureSuccessStatusCode();
        var signed = await signResponse.Content.ReadFromJsonAsync<PrescriptionDto>(JsonOptions);
        Assert.Equal(PrescriptionStatus.Signed, signed!.Status);
        Assert.NotNull(signed.SignedAtUtc);

        // 7. Prove the event actually left the process over the real broker - not just that
        // the in-process publish call didn't throw.
        var delivery = channel.BasicGet(queueName, autoAck: true);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (delivery is null && DateTime.UtcNow < deadline)
        {
            await Task.Delay(100);
            delivery = channel.BasicGet(queueName, autoAck: true);
        }

        Assert.NotNull(delivery);
        var receivedEvent = JsonSerializer.Deserialize<Shared.Events.PrescriptionSignedEvent>(
            Encoding.UTF8.GetString(delivery!.Body.Span), JsonOptions);
        Assert.Equal(prescription.Id, receivedEvent!.PrescriptionId);
    }

    private static async Task SkipUnlessInfrastructureIsReachableAsync(IServiceProvider services)
    {
        var sqlReachable = true;
        try
        {
            using var connection = await services.GetRequiredService<ISqlConnectionFactory>()
                .CreateOpenConnectionAsync();
        }
        catch
        {
            sqlReachable = false;
        }

        var rabbitMqReachable = true;
        try
        {
            var options = services.GetRequiredService<IOptions<RabbitMqOptions>>().Value;
            using var connection = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            }.CreateConnection();
        }
        catch
        {
            rabbitMqReachable = false;
        }

        Skip.IfNot(sqlReachable && rabbitMqReachable,
            "Requires a reachable local SQL Server (ConnectionStrings:ScriptFlowDb) and RabbitMQ " +
            "(see appsettings.Development.json) - not available in CI, skipped there by design.");
    }
}
