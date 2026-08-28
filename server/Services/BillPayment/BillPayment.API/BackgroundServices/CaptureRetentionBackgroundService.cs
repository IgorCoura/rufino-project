namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.Mediator;
using BillPayment.Application.Retention.Commands;
using BillPayment.Domain.Retention;
using Microsoft.Extensions.Options;

/// <summary>
/// Agendador que apaga os registros de e-mail vencidos pela janela de retenção de cada tenant.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Um tenant por escopo, uma transação por tenant</strong> — mesma disciplina da
/// varredura de caixas: um tenant em estado estranho registra o próprio problema e não impede a
/// purga dos outros.
/// </para>
/// <para>
/// Só varre quem <em>ligou</em> a política. Quem nunca abriu a tela não perde histórico, e é por
/// isso que a consulta filtra por <c>IsEnabled</c> em vez de o worker decidir por omissão.
/// </para>
/// </remarks>
internal sealed class CaptureRetentionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<CaptureRetentionOptions> options,
    ILogger<CaptureRetentionBackgroundService> logger) : BackgroundService
{
    private readonly CaptureRetentionOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.Interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Capture retention cycle failed; will retry on next tick.");
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

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        var tenants = await ListEnabledTenantsAsync(stoppingToken);

        foreach (var tenantId in tenants)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            await PurgeOneAsync(tenantId, stoppingToken);
        }
    }

    /// <summary>
    /// Lê as políticas ligadas num escopo próprio e devolve só os <em>ids</em>.
    /// </summary>
    /// <remarks>
    /// Carregar os agregados e reaproveitá-los nos escopos seguintes ligaria entidade ao
    /// <c>DbContext</c> errado. Os ids atravessam; os agregados, não.
    /// </remarks>
    private async Task<List<Guid>> ListEnabledTenantsAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICaptureRetentionPolicyRepository>();

        var policies = await repository.ListEnabledAsync(_options.TenantBatchSize, stoppingToken);

        return policies.Select(p => p.TenantId.Value).ToList();
    }

    private async Task PurgeOneAsync(Guid tenantId, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var result = await mediator.Send(
                new PurgeCapturedMessagesCommand(tenantId, _options.BatchSize), stoppingToken);

            if (result.Purged > 0 && logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Histórico de captura purgado: {Count} registros.", result.Purged);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Um tenant com problema não derruba a purga dos demais.
            logger.LogError(ex, "Falha ao purgar o histórico de captura de um cliente.");
        }
    }
}
