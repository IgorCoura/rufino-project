namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.CaptureItems;
using Microsoft.Extensions.Options;

/// <summary>
/// Processa os artefatos que a sincronização ingeriu: baixa, extrai, tria.
/// </summary>
/// <remarks>
/// <para>
/// Separado do <see cref="CaptureSyncBackgroundService"/> de propósito. Varrer caixa e processar
/// artefato têm ritmos e modos de falha diferentes: a varredura é uma chamada leve por fonte a
/// cada minuto; o processamento baixa megabytes e roda extração. Juntá-los faria um anexo lento
/// atrasar a varredura de todas as caixas — e é a varredura que garante que nada fica para trás.
/// </para>
/// <para>
/// <strong>Um item por escopo, uma transação por item.</strong> Mesma disciplina do outbox: um
/// artefato que estoura registra a própria falha e não impede os outros.
/// </para>
/// </remarks>
internal sealed class CaptureProcessingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<CaptureSyncOptions> options,
    IOptions<CaptureRetryOptions> retryOptions,
    TimeProvider clock,
    ILogger<CaptureProcessingBackgroundService> logger) : BackgroundService
{
    private readonly CaptureSyncOptions _options = options.Value;
    private readonly CaptureRetryOptions _retry = retryOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_options.ProcessingInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var handled = await RunCycleAsync(stoppingToken);

                // Lote cheio provavelmente significa mais fila esperando: volta já, em vez de
                // dormir com trabalho acumulado.
                if (handled >= _options.ProcessingBatchSize)
                    continue;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Capture processing cycle failed; will retry on next tick.");
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

    private async Task<int> RunCycleAsync(CancellationToken stoppingToken)
    {
        var pending = await ListPendingAsync(stoppingToken);

        if (pending.Count == 0)
            return 0;

        // Concorrente, com teto. Cada artefato já roda no próprio escopo e na própria transação,
        // então paralelizar não compartilha DbContext nem agregado — o que muda é só quantos
        // esperam rede ao mesmo tempo. A extração por IA NÃO passa por aqui: ela tem worker
        // próprio, justamente para não haver item lento no meio deste lote.
        await Parallel.ForEachAsync(
            pending,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = Math.Max(1, _options.ProcessingConcurrency),
                CancellationToken = stoppingToken,
            },
            async (item, token) => await ProcessOneAsync(item, token));

        return pending.Count;
    }

    private async Task<IReadOnlyList<PendingCaptureItem>> ListPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemWorkQueries>();

        // Reivindicar, e não apenas listar: o aluguel é o que impede dois workers de pegarem o
        // mesmo artefato, e é o que faz um item voltar sozinho se este processo morrer no meio.
        return await queries.ClaimPendingAsync(
            _options.ProcessingBatchSize,
            clock.GetUtcNow().Add(_retry.LeaseDuration),
            stoppingToken);
    }

    private async Task ProcessOneAsync(PendingCaptureItem item, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var result = await mediator.Send(
                new ProcessCaptureItemCommand(item.TenantId, item.CaptureItemId), stoppingToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Capture item {ItemId} processed: {Decision} ({Instruments} instruments).",
                    item.CaptureItemId,
                    result.Decision,
                    result.InstrumentsFound);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Um artefato que estoura não pode levar os outros junto — mas também não pode
            // voltar à fila para sempre. Quem decide entre insistir e desistir é o agregado,
            // com o contador de tentativas; aqui só se classifica a falha e se registra.
            logger.LogError(ex, "Capture item {ItemId} failed to process.", item.CaptureItemId);

            await CaptureFailureHandling.RecordAsync(scopeFactory, item, ex, logger, stoppingToken);
        }
    }
}
