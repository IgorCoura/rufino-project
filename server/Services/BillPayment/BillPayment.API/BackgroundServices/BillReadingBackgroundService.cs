namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.Bills.Commands;
using BillPayment.Application.CaptureItems.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.Queries.Bills;
using BillPayment.Domain.Bills;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Options;

/// <summary>
/// A fila da leitura por IA dos boletos — a faixa mais lenta de todas.
/// </summary>
/// <remarks>
/// <para>
/// <strong>O boleto nunca espera por ela.</strong> Ele já está em aprovação com o que o funil
/// determinístico provou; o que esta fila entrega é o retrato do documento, que enriquece a
/// decisão e alimenta o check 13. Se o provedor estiver fora do ar por um dia, o pagamento
/// continua acontecendo — só a análise fica pendente, e a tela diz isso em vez de mentir que o
/// documento não tem o que ler.
/// </para>
/// <para>
/// <strong>Serial, como a faixa de visão da captura</strong>, e pelo mesmo motivo já registrado:
/// o teto é a cota do provedor, não o código. Paralelizar aqui troca espera por <c>429</c>.
/// </para>
/// <para>
/// <strong>Vem depois da captura na ordem de prioridade.</strong> A faixa de visão da captura
/// decide se um documento vira boleto; esta enriquece um boleto que já existe. Com cota escassa,
/// a primeira ganha — por isso o intervalo daqui é mais folgado.
/// </para>
/// </remarks>
internal sealed class BillReadingBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<BillReadingOptions> options,
    TimeProvider clock,
    ILogger<BillReadingBackgroundService> logger) : BackgroundService
{
    private readonly BillReadingOptions _options = options.Value;

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
                logger.LogError(ex, "Bill reading cycle failed; will retry on next tick.");
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

        foreach (var pending in claimed)
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            await ReadOneAsync(pending, stoppingToken);
        }
    }

    /// <summary>
    /// Reivindica o lote num escopo próprio e devolve só os <em>ids</em>.
    /// </summary>
    /// <remarks>
    /// O aluguel é marcado no mesmo comando da escolha — ver <c>BillReadingWorkQueries</c>. Os
    /// agregados não atravessam escopos: eles ficariam ligados ao <c>DbContext</c> errado.
    /// </remarks>
    private async Task<IReadOnlyList<PendingBillReading>> ClaimAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var queries = scope.ServiceProvider.GetRequiredService<IBillReadingWorkQueries>();

        var leaseUntil = clock.GetUtcNow().Add(_options.LeaseDuration);

        return await queries.ClaimPendingReadingsAsync(_options.BatchSize, leaseUntil, stoppingToken);
    }

    private async Task ReadOneAsync(PendingBillReading pending, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(
                new ApplyBillReadingCommand(pending.TenantId, pending.BillId), stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await RecordFailureAsync(pending, ex, stoppingToken);
        }
    }

    /// <summary>
    /// Anota a falha num escopo NOVO, e decide se o boleto desiste da análise.
    /// </summary>
    /// <remarks>
    /// <strong>O escopo tem de ser novo</strong> — o que estourou tem um <c>DbContext</c> com o
    /// agregado meio mutado, e gravar por cima persistiria um estado que nunca foi válido. É a
    /// mesma lição da fila de captura.
    /// </remarks>
    private async Task RecordFailureAsync(
        PendingBillReading pending, Exception failure, CancellationToken stoppingToken)
    {
        try
        {
            using var scope = scopeFactory.CreateScope();
            var bills = scope.ServiceProvider.GetRequiredService<IBillRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var bill = await bills.GetAsync(
                TenantId.From(pending.TenantId), BillId.From(pending.BillId), stoppingToken);

            if (bill is null)
                return;

            var gaveUp = bill.RecordReadingFailure(
                IsPermanent(failure),
                _options.MaxAttempts,
                _options.RetryBaseDelay,
                clock.GetUtcNow().UtcDateTime);

            await unitOfWork.SaveEntitiesAsync(stoppingToken);

            if (gaveUp)
                logger.LogWarning(failure, "A leitura por IA deste boleto desistiu após as tentativas.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Falhar ao registrar a falha não pode derrubar o ciclo: o aluguel vence sozinho e o
            // boleto volta à fila.
            logger.LogError(ex, "Não foi possível registrar a falha da leitura por IA.");
        }
    }

    /// <summary>
    /// Indisponibilidade do provedor é <strong>passageira</strong>; o resto é permanente.
    /// </summary>
    /// <remarks>
    /// Inverte a regra da fila de captura de propósito: lá quase toda exceção é passageira e o
    /// <c>DomainException</c> é a exceção; aqui o caminho normal já devolve desfecho sem lançar,
    /// então o que chega como exceção é ou o sinal de "volte para a fila" (<c>BLP.BIL28</c>) ou um
    /// defeito de verdade — e defeito não melhora com repetição.
    /// </remarks>
    private static bool IsPermanent(Exception failure)
        => failure is not DomainException domain
            || !string.Equals(domain.Id, ReadingUnavailableErrorId, StringComparison.Ordinal);

    /// <summary><c>BillErrors.ReadingUnavailable</c>.</summary>
    private const string ReadingUnavailableErrorId = "BLP.BIL28";
}
