namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.Services;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Um humano escolhe a data e manda o boleto aprovado para a fila de pagamento (ADR-018).
/// </summary>
/// <remarks>
/// <para>
/// <strong>É o ato que move dinheiro.</strong> A aprovação autoriza; este comando executa. Por
/// isso a alçada dele na borda é a de agendamento (<c>bill:schedule</c>), não a de aprovação —
/// quem aprova não necessariamente manda pagar.
/// </para>
/// <para>
/// Serve também ao reagendamento: um boleto que teve o agendamento cancelado volta a
/// <c>Approved</c> sem data, e cai exatamente aqui de novo, sem passar por nova aprovação.
/// </para>
/// </remarks>
public sealed record ScheduleBillCommand(
    Guid TenantId,
    Guid BillId,
    Guid UserId,
    DateOnly ScheduleFor,
    string? ActorName = null,
    bool AcknowledgeImmediateExecution = false) : ITenantScopedCommand, IRequest<ScheduleBillResponse>;

public sealed record ScheduleBillResponse(Guid Id, string Status, DateOnly ScheduledFor);

public sealed class ScheduleBillCommandHandler(
    IBillRepository bills,
    IOptions<ApprovalOptions> options,
    IOptions<PaymentSchedulingOptions> schedulingOptions,
    IWorkingDayCalendar calendar,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ScheduleBillCommand, ScheduleBillResponse>
{
    public async Task<ScheduleBillResponse> Handle(ScheduleBillCommand request, CancellationToken cancellationToken)
    {
        var bill = await bills.GetAsync(
                TenantId.From(request.TenantId), BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var now = clock.GetUtcNow();
        var scheduling = schedulingOptions.Value;

        // "Agora" no fuso da POLÍTICA (America/Sao_Paulo), não em UTC: entre ~21h e meia-noite
        // locais o dia UTC já virou, e a guarda de vencido (BIL35) diria "vencido" de um boleto
        // que vence hoje — a tela (que vive no dia local) nem mostraria a caixa de aceite. A
        // HORA local importa desde o ADR-021: é ela que decide se hoje ainda serve como data.
        var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(now.UtcDateTime, scheduling.ResolveTimeZone());
        var today = DateOnly.FromDateTime(nowLocal);

        var sameDay = PaymentSchedulingService.CanScheduleForToday(
            bill.DueDate, bill.Lookup?.MinimumScheduleDate, nowLocal, scheduling.ToPolicy(), calendar);

        // Estado exigido, frescor do retrato, data permitida e aceite de execução imediata: tudo
        // no método rico. O handler resolve política, fuso e calendário, e nada mais.
        bill.Schedule(
            UserId.From(request.UserId),
            request.ScheduleFor,
            options.Value.ToPolicy(),
            today,
            sameDay,
            now.UtcDateTime,
            request.AcknowledgeImmediateExecution,
            request.ActorName);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ScheduleBillResponse(bill.Id.Value, bill.Status.Name, bill.ScheduledFor!.Value);
    }
}

public sealed class ScheduleBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ScheduleBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ScheduleBillCommand, ScheduleBillResponse>(mediator, requestManager, logger)
{
    protected override ScheduleBillResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, default);
}
