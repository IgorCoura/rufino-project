namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Passa uma expectativa pelo dia de hoje: abre o ciclo devido, alerta o que precisa ser
/// alertado, dá por não cumprido o que passou do prazo e desativa o que morreu de silêncio.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Uma expectativa por comando, uma transação por expectativa</strong> — mesma disciplina
/// da varredura de caixas e do outbox. Uma expectativa em estado estranho registra o próprio
/// problema e não impede as outras de serem varridas.
/// </para>
/// <para>
/// <strong>O alerta é gravado no agregado antes de ser enviado, e nunca no sentido inverso.</strong>
/// O registro é o que sustenta a regra de não repetir nível e o painel de pendências; o envio
/// acontece depois, pelo outbox. Se o canal estiver fora do ar, o painel continua contando a
/// verdade — e se o registro dependesse do envio, um canal instável faria o mesmo alerta sair
/// todo dia.
/// </para>
/// </remarks>
public sealed record SweepBillExpectationCommand(Guid TenantId, Guid ExpectationId)
    : IRequest<SweepBillExpectationResponse>;

/// <param name="Outcome">
/// <c>Idle</c>, <c>CycleOpened</c>, <c>Alerted</c>, <c>Missed</c> ou <c>Deactivated</c> — o
/// desfecho mais significativo da passagem.
/// </param>
public sealed record SweepBillExpectationResponse(Guid Id, string Outcome, string? AlertLevel = null);

public sealed class SweepBillExpectationCommandHandler(
    IBillExpectationRepository expectations,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<SweepBillExpectationCommandHandler> logger)
    : IRequestHandler<SweepBillExpectationCommand, SweepBillExpectationResponse>
{
    private const string OUTCOME_IDLE = "Idle";
    private const string OUTCOME_CYCLE_OPENED = "CycleOpened";
    private const string OUTCOME_ALERTED = "Alerted";
    private const string OUTCOME_MISSED = "Missed";
    private const string OUTCOME_DEACTIVATED = "Deactivated";

    public async Task<SweepBillExpectationResponse> Handle(
        SweepBillExpectationCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var expectation = await expectations.GetAsync(
                tenantId, BillExpectationId.From(request.ExpectationId), cancellationToken)
            ?? throw BillExpectationErrors.NotFound(request.ExpectationId);

        var now = clock.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        if (!expectation.IsWatchingOn(today))
            return new SweepBillExpectationResponse(expectation.Id.Value, OUTCOME_IDLE);

        var outcome = OUTCOME_IDLE;
        string? level = null;

        if (OpenDueCycle(expectation, today, now))
            outcome = OUTCOME_CYCLE_OPENED;

        var cycle = CurrentCycle(expectation, today);

        if (cycle is not null)
        {
            var due = AlertLevel.DueOn(today, cycle.AlertAt, cycle.ExpectedDueDate);

            if (due is not null)
            {
                // Marcar como não cumprido ANTES de registrar o alerta: é o estado do ciclo que
                // decide qual dos dois avisos sai — "não chegou" ou "chegou e não deu para ler".
                if (cycle.Status == CycleStatus.Waiting)
                {
                    expectation.MarkMissing(cycle.Id, MissReason.NeverArrived, today, now);
                    outcome = OUTCOME_MISSED;
                }

                if (expectation.TryRecordAlert(cycle.Id, due, now))
                {
                    outcome = outcome == OUTCOME_MISSED ? OUTCOME_MISSED : OUTCOME_ALERTED;
                    level = due.Name;
                }
            }
        }

        // Silêncio do usuário diante de três alertas seguidos é sinal de que a expectativa morreu
        // — o imóvel foi vendido, o contrato encerrou. Continuar alertando treinaria a pessoa a
        // ignorar alerta, e é isso que destrói o mecanismo.
        if (expectation.ShouldDeactivateForSilence())
        {
            expectation.Deactivate("silence_after_consecutive_misses", now);
            outcome = OUTCOME_DEACTIVATED;

            if (logger.IsEnabled(LogLevel.Information))
            {
                logger.LogInformation(
                    "Expectativa desativada após {Count} ciclos consecutivos sem cumprimento.",
                    BillExpectation.CONSECUTIVE_MISSES_TO_DEACTIVATE);
            }
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new SweepBillExpectationResponse(expectation.Id.Value, outcome, level);
    }

    /// <summary>
    /// Abre o ciclo da competência corrente quando ele ainda não existe e já está na hora.
    /// </summary>
    /// <remarks>
    /// A hora é <c>AlertLeadDays</c> antes do vencimento esperado: abrir muito antes encheria o
    /// painel de ciclos que ninguém pode resolver ainda, e abrir depois perderia a janela do
    /// primeiro alerta.
    /// </remarks>
    private static bool OpenDueCycle(BillExpectation expectation, DateOnly today, DateTime now)
    {
        var competence = new CompetencePeriod(today.Year, today.Month);

        if (expectation.CycleFor(competence) is not null)
            return false;

        var dueDate = expectation.DueDateIn(competence);

        if (today < dueDate.AddDays(-expectation.AlertLeadDays))
            return false;

        expectation.OpenCycle(competence, now);
        return true;
    }

    /// <summary>
    /// O ciclo mais relevante para hoje: o aberto mais antigo que já pede atenção.
    /// </summary>
    /// <remarks>
    /// O mais antigo, e não o da competência corrente, porque um ciclo vencido e não resolvido
    /// continua sendo o que importa — deixar de escalá-lo porque o mês virou seria abandonar
    /// justamente a conta que já está atrasada.
    /// </remarks>
    private static ExpectationCycle? CurrentCycle(BillExpectation expectation, DateOnly today)
        => expectation.Cycles
            .Where(c => c.Status.IsOpen && today >= c.AlertAt)
            .OrderBy(c => c.ExpectedDueDate)
            .FirstOrDefault();
}

public sealed class SweepBillExpectationIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<SweepBillExpectationIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<SweepBillExpectationCommand, SweepBillExpectationResponse>(
        mediator, requestManager, logger)
{
    protected override SweepBillExpectationResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, null);
}
