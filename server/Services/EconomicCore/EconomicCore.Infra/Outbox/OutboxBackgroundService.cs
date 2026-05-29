namespace EconomicCore.Infra.Outbox;

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

internal sealed class OutboxBackgroundService : BackgroundService
{
    private readonly IOutboxProcessor _processor;
    private readonly OutboxOptions _options;
    private readonly ILogger<OutboxBackgroundService> _logger;

    public OutboxBackgroundService(
        IOutboxProcessor processor,
        IOptions<OutboxOptions> options,
        ILogger<OutboxBackgroundService> logger)
    {
        _processor = processor;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollingInterval);
        var cyclesSinceCleanup = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var handled = await _processor.ProcessPendingAsync(stoppingToken);

                // Batch came back full — likely more pending, so poll again immediately.
                if (handled >= _options.BatchSize)
                    continue;

                if (++cyclesSinceCleanup >= _options.CleanupEveryCycles)
                {
                    await _processor.CleanupAsync(stoppingToken);
                    cyclesSinceCleanup = 0;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Outbox processing cycle failed; will retry on next tick.");
            }

            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
