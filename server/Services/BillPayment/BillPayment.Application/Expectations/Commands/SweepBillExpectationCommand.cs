namespace BillPayment.Application.Expectations.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Expectations;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Passa uma expectativa pelo dia de hoje: abre os ciclos devidos, alerta o que precisa ser
/// alertado, dá por não cumprido o que passou do prazo e desativa o que morreu de silêncio.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Uma expectativa por comando, uma transação por expectativa</strong> — mesma disciplina
/// da varredura de caixas e do outbox. Uma expectativa em estado estranho registra o próprio
/// problema e não impede as outras de serem varridas.
/// </para>
/// <para>
/// <strong>Toda passagem carimba <c>LastSweptAt</c>, inclusive a que não faz nada.</strong> É o
/// carimbo que faz a fila girar: sem ele, a expectativa parada mantinha o <c>UpdatedAt</c> antigo
/// e ocupava as vagas do lote para sempre, enquanto a que estava sendo cumprida ia para o fim.
/// Por isso o comando persiste mesmo quando o desfecho é <c>Idle</c>.
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

        // Antes de qualquer desvio: mesmo a passagem que nada faz precisa sair da frente da fila.
        expectation.MarkSwept(now);

        if (!expectation.IsWatchingOn(today))
        {
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new SweepBillExpectationResponse(expectation.Id.Value, OUTCOME_IDLE);
        }

        var outcome = OUTCOME_IDLE;
        string? level = null;

        // Quem decide quais competências abrir é o agregado: a cadência, o prazo de chegada e o
        // piso de vigilância são dele, e replicar essa escolha aqui seria um segundo lugar para
        // ela envelhecer.
        if (expectation.OpenDueCycles(today, now).Count > 0)
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
    /// O ciclo mais relevante para hoje: o aberto mais antigo que já pede atenção.
    /// </summary>
    /// <remarks>
    /// O mais antigo, e não o da competência corrente, porque um ciclo vencido e não resolvido
    /// continua sendo o que importa — deixar de escalá-lo porque o mês virou seria abandonar
    /// justamente a conta que já está atrasada. Ciclos abertos à frente (a conta ainda vai chegar)
    /// ficam de fora pela data de alerta, que ainda não chegou.
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
