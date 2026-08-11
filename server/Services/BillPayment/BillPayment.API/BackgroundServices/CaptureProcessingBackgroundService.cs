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
    ILogger<CaptureProcessingBackgroundService> logger) : BackgroundService
{
    private readonly CaptureSyncOptions _options = options.Value;

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
        var handled = 0;

        foreach (var item in pending)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await ProcessOneAsync(item, stoppingToken);
            handled++;
        }

        return handled;
    }

    private async Task<IReadOnlyList<PendingCaptureItem>> ListPendingAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<ICaptureItemWorkQueries>();

        return await queries.ListPendingAsync(_options.ProcessingBatchSize, stoppingToken);
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
            // Um artefato que estoura não pode levar os outros junto. O item permanece em
            // Received e volta no ciclo seguinte — o que é seguro porque o processamento é
            // idempotente: baixar e extrair de novo produz o mesmo desfecho.
            logger.LogError(ex, "Capture item {ItemId} failed to process.", item.CaptureItemId);
        }
    }
}
