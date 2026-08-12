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
/// <strong>O intervalo é diário, e não de minutos.</strong> O que este worker observa muda de dia
/// em dia — a conta não chega "às 14h", ela não chega "hoje" —, e rodar de minuto em minuto só
/// gastaria transação para reencontrar o mesmo estado. O ciclo continua idempotente: alerta já
/// registrado não sai de novo.
/// </para>
/// </remarks>
internal sealed class ExpectationSweepBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ExpectationSweepOptions> options,
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

    private async Task RunCycleAsync(CancellationToken stoppingToken)
    {
        foreach (var (tenantId, expectationId) in await ListDueAsync(stoppingToken))
        {
            if (stoppingToken.IsCancellationRequested)
                return;

            await SweepOneAsync(tenantId, expectationId, stoppingToken);
        }
    }

    /// <summary>
    /// Lê as expectativas devidas num escopo próprio e devolve só os <em>ids</em>.
    /// </summary>
    /// <remarks>
    /// Carregar os agregados e reaproveitá-los nos escopos seguintes os ligaria ao
    /// <c>DbContext</c> errado. Os ids atravessam; os agregados, não.
    /// </remarks>
    private async Task<List<(Guid TenantId, Guid ExpectationId)>> ListDueAsync(CancellationToken stoppingToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IBillExpectationRepository>();

        var due = await repository.ListActiveForSweepAsync(_options.BatchSize, stoppingToken);

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
            // varredura de caixas.
            logger.LogError(ex, "Sweeping one expectation failed; the remaining ones continue.");
        }
    }
}
