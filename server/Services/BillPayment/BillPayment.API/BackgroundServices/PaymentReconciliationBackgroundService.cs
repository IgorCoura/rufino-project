namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Application.Queries.PaymentOrders;
using Microsoft.Extensions.Options;

/// <summary>
/// A rede de segurança do webhook: consulta periodicamente as ordens sem notícia. Ligada por
/// padrão pela mesma doutrina do <c>Expectations:Enabled</c> — o modo de falha do webhook
/// perdido é o silêncio, e dinheiro parado em silêncio é o pior silêncio deste BC.
/// </summary>
internal sealed class PaymentReconciliationBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<PaymentReconciliationOptions> options,
    TimeProvider clock,
    ILogger<PaymentReconciliationBackgroundService> logger) : BackgroundService
{
    private readonly PaymentReconciliationOptions _options = options.Value;

    /// <summary>Ordem → ciclos seguidos sem conseguir conciliar. Só o laço toca; ele é sequencial.</summary>
    private readonly Dictionary<Guid, int> _blockedStreak = [];

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
                logger.LogError(ex, "Payment reconciliation cycle failed; will retry on next tick.");
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
        var cutoff = clock.GetUtcNow() - _options.StaleAfter;

        IReadOnlyList<PendingPaymentSubmission> stale;
        IReadOnlyList<PendingPaymentSubmission> missingReceipts;

        using (var scope = scopeFactory.CreateScope())
        {
            var queries = scope.ServiceProvider.GetRequiredService<IPaymentOrderWorkQueries>();
            stale = await queries.ClaimStaleAwaitingProviderAsync(cutoff, _options.BatchSize, stoppingToken);
            missingReceipts = await queries.ClaimPaidMissingReceiptAsync(cutoff, _options.BatchSize, stoppingToken);
        }

        foreach (var pending in stale)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                var result = await mediator.Send(
                    new ReconcilePaymentOrderCommand(pending.TenantId, pending.PaymentOrderId), stoppingToken);

                TrackProgress(pending.PaymentOrderId, result);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Uma ordem que não concilia não pode impedir as outras — e o carimbo do claim
                // já a mandou para o fim da fila do próximo lote (anti-inanição).
                logger.LogError(
                    ex, "Não foi possível conciliar a ordem {PaymentOrderId}.", pending.PaymentOrderId);

                TrackProgress(
                    pending.PaymentOrderId,
                    new ReconcilePaymentOrderResponse(
                        pending.PaymentOrderId, ReconcilePaymentOrderResponse.OUTCOME_THREW));
            }
        }

        // A rede de segurança do comprovante: o outbox tem backoff FINITO — esgotado ele, uma
        // ordem paga sem comprovante nunca mais seria tentada. Esta é a segunda olhada,
        // atrasada e DEFINITIVA: sem URL no provedor, a marca tira a ordem da varredura.
        foreach (var pending in missingReceipts)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            using var scope = scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            try
            {
                await mediator.Send(
                    new CapturePaymentReceiptCommand(pending.TenantId, pending.PaymentOrderId, Definitive: true),
                    stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // BLP.PMO21 (provedor fora do ar) cai aqui: o carimbo do claim pace a próxima
                // tentativa — sem laço quente, sem inanição das demais.
                logger.LogError(ex, "Não foi possível capturar o comprovante de uma ordem paga.");
            }
        }
    }

    /// <summary>
    /// Conta quantos ciclos seguidos uma ordem falhou em ser conciliada, e grita ao cruzar a régua.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Existe porque o silêncio quase escondeu um pagamento.</strong> Em 2026-09-09 uma
    /// ordem paga de verdade no provedor ficou horas em <c>Pending</c>: o retrato vinha
    /// incoerente, a aplicação lançava, e o laço registrava a falha como mais uma linha de erro —
    /// indistinguível de um soluço passageiro. Uma ordem que falha ciclo após ciclo é outra
    /// classe de problema, e precisa dizer isso.
    /// </para>
    /// <para>
    /// A contagem é em MEMÓRIA e some no restart, de propósito: é sinal operacional, não estado
    /// de negócio — persisti-la pediria coluna, migração e limpeza para um dado que só serve
    /// enquanto o processo está de pé.
    /// </para>
    /// </remarks>
    private void TrackProgress(Guid paymentOrderId, ReconcilePaymentOrderResponse result)
    {
        if (!result.IsBlocked)
        {
            _blockedStreak.Remove(paymentOrderId);
            return;
        }

        var streak = _blockedStreak.TryGetValue(paymentOrderId, out var current) ? current + 1 : 1;
        _blockedStreak[paymentOrderId] = streak;

        if (streak < _options.BlockedStreakAlertThreshold)
            return;

        logger.LogError(
            "A ordem {PaymentOrderId} falhou a conciliação {Streak} ciclos seguidos (último desfecho: "
            + "{Outcome}). Se o provedor já executou o pagamento, o espelho está mentindo — "
            + "confira no painel dele.",
            paymentOrderId, streak, result.Outcome);
    }
}

/// <summary>O ritmo da conciliação. A regra de negócio dela mora no comando, não aqui.</summary>
public sealed class PaymentReconciliationOptions
{
    public const string SectionName = "PaymentReconciliation";

    public bool Enabled { get; set; } = true;

    public TimeSpan Interval { get; set; } = TimeSpan.FromMinutes(10);

    /// <summary>Há quanto tempo sem notícia uma ordem pendente vira alvo de consulta.</summary>
    public TimeSpan StaleAfter { get; set; } = TimeSpan.FromHours(1);

    public int BatchSize { get; set; } = 50;

    /// <summary>
    /// Quantos ciclos seguidos uma ordem pode falhar a conciliação antes de o alerta subir.
    /// </summary>
    /// <remarks>
    /// Três é meia hora no ritmo padrão: passageiro já passou, e o que sobra merece gente olhando.
    /// </remarks>
    public int BlockedStreakAlertThreshold { get; set; } = 3;
}
