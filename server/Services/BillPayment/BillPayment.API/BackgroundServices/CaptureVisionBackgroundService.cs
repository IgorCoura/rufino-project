namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.CaptureItems;
using Microsoft.Extensions.Options;

/// <summary>
/// A faixa lenta: retoma os artefatos que só o extrator de IA pode resolver.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o terceiro worker de captura, e existe pela mesma razão dos dois primeiros.</strong>
/// Varrer caixa, processar artefato e chamar a IA têm ritmos incompatíveis: a varredura é uma
/// chamada leve por fonte; o processamento comum leva 150 ms; a extração por IA leva de 3 a 5
/// segundos e disputa uma cota limitada por minuto. Medido em 2026-08-26, com os três no mesmo
/// laço: <strong>27% dos itens consumiam 86% do tempo</strong>, e a fila inteira ficava atrás
/// deles.
/// </para>
/// <para>
/// <strong>Serial de propósito, ao contrário da faixa rápida.</strong> Concorrência aqui não
/// compra nada: o teto é da conta no provedor, não do código — mandar em paralelo só troca espera
/// por <c>429</c>, e o cliente de visão não retenta (cada tentativa consome cota). Um por vez, no
/// ritmo que a cota permite.
/// </para>
/// <para>
/// O item é rebaixado do provedor quando chega aqui — os degraus 0 a 2 rodam de novo antes do 3.
/// Custa os mesmos 150 a 360 ms do caminho comum, contra os segundos que ele deixou de bloquear
/// ao ceder a vez.
/// </para>
/// </remarks>
internal sealed class CaptureVisionBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<CaptureSyncOptions> options,
    IOptions<CaptureRetryOptions> retryOptions,
    TimeProvider clock,
    ILogger<CaptureVisionBackgroundService> logger) : BackgroundService
{
    private readonly CaptureSyncOptions _options = options.Value;
    private readonly CaptureRetryOptions _retry = retryOptions.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var handled = 0;

            try
            {
                handled = await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Capture vision cycle failed; will retry on next tick.");
            }

            try
            {
                // Lote cheio significa fila com mais coisa: emenda o próximo, em vez de dormir
                // sobre trabalho pendente. Quem segura o ritmo de verdade é a cota do provedor,
                // dentro da chamada — não este intervalo.
                await Task.Delay(
                    handled >= _options.VisionBatchSize ? _options.VisionCatchUpInterval : _options.VisionInterval,
                    stoppingToken);
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

        // Aluguel mais longo que o da faixa rapida: aqui uma chamada leva de 3 a 5 s e ainda
        // pode esperar cota. Um aluguel curto venceria com o worker vivo, e outro ciclo pegaria
        // o mesmo artefato para gastar a cota duas vezes no mesmo documento.
        return await queries.ClaimPendingVisionAsync(
            _options.VisionBatchSize,
            clock.GetUtcNow().Add(_retry.VisionLeaseDuration),
            stoppingToken);
    }

    private async Task ProcessOneAsync(PendingCaptureItem item, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            // VisionLane: é este worker, e só ele, que tem licença para gastar a cota de IA.
            var result = await mediator.Send(
                new ProcessCaptureItemCommand(item.TenantId, item.CaptureItemId, VisionLane: true),
                stoppingToken);

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Capture item {ItemId} left the vision queue as {Decision} ({Instruments} instruments).",
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
            // Um artefato que estoura não pode levar os outros junto — mesma disciplina do outbox.
            // E também não pode voltar para sempre: com lote de 5, bastavam cinco itens presos
            // para a fila da IA parar por completo, sem erro visível em tela nenhuma.
            logger.LogError(ex, "Capture item {ItemId} failed in the vision queue.", item.CaptureItemId);

            await CaptureFailureHandling.RecordAsync(scopeFactory, item, ex, logger, stoppingToken);
        }
    }
}
