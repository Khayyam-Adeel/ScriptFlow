using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ScriptFlow.API.Infrastructure.Expiry;

/// <summary>Runs PrescriptionExpiryService.RunSweepAsync on a fixed interval
/// (PrescriptionExpiryOptions.IntervalMinutes) for the lifetime of the host.</summary>
public sealed class PrescriptionExpiryBackgroundService : BackgroundService
{
    private readonly PrescriptionExpiryService _expiryService;
    private readonly IOptions<PrescriptionExpiryOptions> _options;
    private readonly ILogger<PrescriptionExpiryBackgroundService> _logger;

    public PrescriptionExpiryBackgroundService(
        PrescriptionExpiryService expiryService,
        IOptions<PrescriptionExpiryOptions> options,
        ILogger<PrescriptionExpiryBackgroundService> logger)
    {
        _expiryService = expiryService;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_options.Value.IntervalMinutes));

        do
        {
            try
            {
                await _expiryService.RunSweepAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // One failed sweep (a transient DB blip, RabbitMQ unreachable) must not stop
                // every future sweep for the rest of the process's lifetime - log and retry
                // on the next tick, same resilience approach as the RabbitMQ consumers.
                _logger.LogError(ex, "Prescription expiry sweep failed; will retry next interval");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
