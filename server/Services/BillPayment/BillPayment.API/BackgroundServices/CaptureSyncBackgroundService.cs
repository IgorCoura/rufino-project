namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.CaptureSources.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.CaptureSources;
using Microsoft.Extensions.Options;

/// <summary>
/// Agendador que varre as caixas habilitadas da instalação.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Vive na API, e não na Infra como o worker do outbox.</strong> A diferença não é
/// estilística: o outbox é infraestrutura pura — move linhas de uma tabela e despacha por uma
/// porta do Domain, sem tocar em caso de uso. Sincronizar uma caixa <em>é</em> um caso de uso, e
/// mora na Application; um <c>BackgroundService</c> na Infra teria de alcançar o mediator, e
/// <c>Infra → Application</c> é ciclo. A API é o composition host e enxerga as duas.
/// </para>
/// <para>
/// <strong>Uma fonte por escopo, uma transação por fonte.</strong> Mesma disciplina do outbox
/// reivindicando uma mensagem por vez: uma caixa fora do ar registra a própria falha e não
/// impede as outras de sincronizar.
/// </para>
/// </remarks>
internal sealed class CaptureSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<CaptureSyncOptions> options,
    ILogger<CaptureSyncBackgroundService> logger) : BackgroundService
{
    private readonly CaptureSyncOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.PollingInterval);

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
                logger.LogError(ex, "Capture sync cycle failed; will retry on next tick.");
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
        var due = await ListDueAsync(stoppingToken);

        foreach (var (tenantId, sourceId) in due)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            await SyncOneAsync(tenantId, sourceId, stoppingToken);
        }
    }

    /// <summary>
    /// Lê as fontes devidas num escopo próprio, e devolve só os <em>ids</em>.
    /// </summary>
    /// <remarks>
    /// Carregar as entidades e reaproveitá-las nos escopos seguintes ligaria agregados ao
    /// <c>DbContext</c> errado. Os ids atravessam; os agregados, não.
    /// </remarks>
    private async Task<List<(Guid TenantId, Guid SourceId)>> ListDueAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ICaptureSourceRepository>();

        var sources = await repository.ListEnabledForWorkerAsync(_options.BatchSize, stoppingToken);

        return sources
            .Where(s => s.Kind.SupportsIncrementalSync)
            .Select(s => (s.TenantId.Value, s.Id.Value))
            .ToList();
    }

    private async Task SyncOneAsync(Guid tenantId, Guid sourceId, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var result = await mediator.Send(new SyncCaptureSourceCommand(tenantId, sourceId), stoppingToken);

            // Varredura que não trouxe nada é o caso comum — registrá-la a cada minuto por fonte
            // afogaria justamente o ciclo em que algo aconteceu.
            if (result.IngestedItems > 0 && logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Capture source {SourceId} ingested {Ingested} new items ({Skipped} already known).",
                    sourceId,
                    result.IngestedItems,
                    result.SkippedAsAlreadyIngested);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Uma fonte que estoura não pode levar as outras junto — a falha de leitura já é
            // modelada e gravada no agregado; o que chega aqui é defeito, e é isolado por fonte.
            logger.LogError(ex, "Capture source {SourceId} failed to sync.", sourceId);
        }
    }
}
