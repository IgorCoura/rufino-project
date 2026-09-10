namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.Bills.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.Bills;
using BillPayment.Domain.SeedWork;
using Microsoft.Extensions.Options;

/// <summary>
/// Reconsulta sozinho os boletos que ficaram sem consulta oficial, até a consulta voltar.
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o par obrigatório da decisão de 2026-09-10.</strong> Consulta sem resposta passou a
/// valer Extremo Perigo, e a tela promete que revalidar mais tarde derruba a classificação. Sem
/// este worker essa promessa seria trabalho manual que ninguém faz, e o boleto ficaria exigindo a
/// alçada máxima por um incidente que já terminou.
/// </para>
/// <para>
/// <strong>Reusa o caso de uso de sempre</strong> (<c>ValidateBillCommand</c>, o mesmo do botão
/// Revalidar). Um segundo caminho de validação seria um segundo lugar para as regras envelhecerem.
/// </para>
/// <para>
/// <strong>Serial, e com aborto precoce.</strong> O cliente de consulta tem disjuntor por cliente
/// nomeado: martelar um provedor fora do ar o abre e derruba junto as validações interativas de
/// quem está usando a tela. Se as primeiras do ciclo voltarem indisponíveis, o ciclo para — o
/// provedor está fora, e insistir no lote inteiro não ensina nada.
/// </para>
/// </remarks>
internal sealed class BillRevalidationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<BillRevalidationOptions> options,
    ILogger<BillRevalidationBackgroundService> logger) : BackgroundService
{
    private readonly BillRevalidationOptions _options = options.Value;

    /// <summary>Ciclos seguidos que reivindicaram boleto e não conseguiram resolver nenhum.</summary>
    private int _blockedStreak;

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
                logger.LogError(ex, "Bill revalidation cycle failed; will retry on next tick.");
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
        var claimed = await ClaimAsync(stoppingToken);
        if (claimed.Count == 0)
        {
            _blockedStreak = 0;
            return;
        }

        var unavailableStreak = 0;
        var resolvedAny = false;

        foreach (var pending in claimed)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            var outcome = await RevalidateOneAsync(pending, stoppingToken);

            if (outcome == CycleOutcome.Resolved)
            {
                resolvedAny = true;
                unavailableStreak = 0;
                continue;
            }

            if (outcome != CycleOutcome.StillUnavailable)
                continue;

            if (++unavailableStreak < _options.UnavailableStreakAbort)
                continue;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Consulta oficial indisponível em {Streak} boletos seguidos; o ciclo de revalidação para aqui.",
                    unavailableStreak);
            }

            break;
        }

        TrackBlockedStreak(claimed.Count, resolvedAny);
    }

    /// <summary>
    /// Não há teto de tentativas — o que existe é alerta. Desistir deixaria o boleto em Extremo
    /// Perigo para sempre por causa de uma queda que já passou.
    /// </summary>
    private void TrackBlockedStreak(int claimedCount, bool resolvedAny)
    {
        if (resolvedAny)
        {
            _blockedStreak = 0;
            return;
        }

        _blockedStreak++;

        if (_blockedStreak >= _options.BlockedStreakAlertThreshold && logger.IsEnabled(LogLevel.Warning))
        {
            logger.LogWarning(
                "A consulta oficial não responde há {Cycles} ciclos de revalidação; {Count} boleto(s) seguem sem verificação.",
                _blockedStreak,
                claimedCount);
        }
    }

    private async Task<IReadOnlyList<PendingBillRevalidation>> ClaimAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillRevalidationWorkQueries>();

        return await queries.ClaimStaleWithoutLookupAsync(
            _options.BatchSize, _options.RetryBaseDelay, _options.RetryMaxDelay, stoppingToken);
    }

    private async Task<CycleOutcome> RevalidateOneAsync(
        PendingBillRevalidation pending, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            var response = await mediator.Send(
                new ValidateBillCommand(pending.TenantId, pending.BillId), stoppingToken);

            return response.LookupResolved ? CycleOutcome.Resolved : CycleOutcome.StillUnavailable;
        }
        catch (ConcurrencyConflictException ex)
        {
            // Alguém decidiu sobre o boleto entre a reivindicação e a execução. Não é falha do
            // provedor e não pode contar para o aborto do ciclo.
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(ex, "Revalidação de {BillId} perdeu a corrida para uma decisão.", pending.BillId);

            return CycleOutcome.Skipped;
        }
        catch (DomainException ex)
        {
            // O boleto saiu do estado que aceita revalidação silenciosa na mesma janela. Sai da
            // fila sozinho no ciclo seguinte, porque a verificação 3 deixa de existir para ele.
            if (logger.IsEnabled(LogLevel.Debug))
                logger.LogDebug(ex, "Revalidação de {BillId} recusada pelo domínio.", pending.BillId);

            return CycleOutcome.Skipped;
        }
        catch (Exception ex)
        {
            if (logger.IsEnabled(LogLevel.Error))
            {
                logger.LogError(
                    ex,
                    "Revalidação de {BillId} falhou na tentativa {Attempts}.",
                    pending.BillId,
                    pending.Attempts);
            }

            return CycleOutcome.Skipped;
        }
    }

    /// <summary>O que aconteceu com UM boleto, do ponto de vista do ciclo.</summary>
    private enum CycleOutcome
    {
        /// <summary>A consulta respondeu — o boleto sai da fila sozinho.</summary>
        Resolved,

        /// <summary>A consulta continua sem responder. É o que alimenta o aborto precoce.</summary>
        StillUnavailable,

        /// <summary>Nada foi aprendido sobre o provedor: corrida, recusa de domínio ou defeito nosso.</summary>
        Skipped,
    }
}
