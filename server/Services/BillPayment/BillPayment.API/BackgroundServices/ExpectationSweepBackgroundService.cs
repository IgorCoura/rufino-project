namespace BillPayment.API.BackgroundServices;

using BillPayment.Application.Expectations.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Domain.Expectations;
using Microsoft.Extensions.Options;

/// <summary>
/// Agendador que passa as expectativas ativas pelo dia de hoje.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Uma expectativa por escopo, uma transação por expectativa</strong> — mesma disciplina
/// da varredura de caixas. Uma expectativa em estado estranho registra o próprio problema e não
/// impede as outras.
/// </para>
/// <para>
/// <strong>O ciclo drena a fila, não pega um lote e vai embora.</strong> A consulta corta por
/// <c>LastSweptAt</c> — carimbado em toda passagem —, então pedir lotes em laço termina sozinho
/// quando não sobra ninguém por varrer. Até 2026-08-27 o worker pegava um lote de cem ordenado
/// por <c>UpdatedAt</c> e parava: como a expectativa que nada faz mantém o carimbo antigo, ela
/// monopolizava as vagas, e as demais nunca eram varridas — sem erro em log nenhum.
/// </para>
/// <para>
/// <strong>O intervalo é diário, e não de minutos.</strong> O que este worker observa muda de dia
/// em dia — a conta não chega "às 14h", ela não chega "hoje" —, e rodar de minuto em minuto só
/// gastaria transação para reencontrar o mesmo estado. O ciclo continua idempotente: alerta já
/// registrado não sai de novo.
/// </para>
/// </remarks>
internal sealed class ExpectationSweepBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ExpectationSweepOptions> options,
    TimeProvider clock,
    ILogger<ExpectationSweepBackgroundService> logger) : BackgroundService
{
    private readonly ExpectationSweepOptions _options = options.Value;

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
                logger.LogError(ex, "Expectation sweep cycle failed; will retry on next tick.");
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

    /// <summary>
    /// Varre até a fila secar, em lotes.
    /// </summary>
    /// <remarks>
    /// O instante de início do ciclo é o corte: quem já foi varrido depois dele não volta na
    /// consulta, e é isso que faz o laço terminar sem contador de páginas.
    /// </remarks>
    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        var startedAt = clock.GetUtcNow().UtcDateTime;
        var swept = 0;

        // Expectativa que FALHA não recebe o carimbo — de propósito, para voltar no ciclo
        // seguinte. Mas isso a traz de volta no lote seguinte deste mesmo ciclo, e sem esta
        // memória o laço giraria nela até o teto. O conjunto guarda quem já teve a vez agora.
        var attempted = new HashSet<Guid>();

        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await ListDueAsync(startedAt, stoppingToken);
            var pending = batch.Where(e => !attempted.Contains(e.ExpectationId)).ToList();

            if (pending.Count == 0)
                break;

            foreach (var (tenantId, expectationId) in pending)
            {
                if (stoppingToken.IsCancellationRequested)
                    return;

                attempted.Add(expectationId);
                await SweepOneAsync(tenantId, expectationId, stoppingToken);
                swept++;
            }

            if (swept >= _options.MaxPerCycle)
            {
                // Alcançar o teto é anomalia. Registrar em Warning é o que impede a cobertura
                // parcial de voltar a ser invisível, que foi o defeito de origem.
                logger.LogWarning(
                    "Expectation sweep hit the per-cycle cap of {Cap}; the remainder waits for the next tick.",
                    _options.MaxPerCycle);

                break;
            }
        }

        if (swept > 0 && logger.IsEnabled(LogLevel.Information))
            logger.LogInformation("Expectation sweep covered {Count} expectation(s).", swept);
    }

    /// <summary>
    /// Lê as expectativas devidas num escopo próprio e devolve só os <em>ids</em>.
    /// </summary>
    /// <remarks>
    /// Carregar os agregados e reaproveitá-los nos escopos seguintes os ligaria ao
    /// <c>DbContext</c> errado. Os ids atravessam; os agregados, não.
    /// </remarks>
    private async Task<List<(Guid TenantId, Guid ExpectationId)>> ListDueAsync(
        DateTime notSweptSince, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBillExpectationRepository>();

        var due = await repository.ListActiveForSweepAsync(
            _options.BatchSize, notSweptSince, stoppingToken);

        return due.Select(e => (e.TenantId.Value, e.Id.Value)).ToList();
    }

    private async Task SweepOneAsync(Guid tenantId, Guid expectationId, CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        try
        {
            await mediator.Send(new SweepBillExpectationCommand(tenantId, expectationId), stoppingToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Uma expectativa que falha não pode derrubar o ciclo das outras — mesma razão da
            // varredura de caixas. Ela não recebe o carimbo, então volta no ciclo seguinte.
            logger.LogError(ex, "Sweeping one expectation failed; the remaining ones continue.");
        }
    }
}
