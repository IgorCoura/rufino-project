namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Um humano autoriza o pagamento — e, opcionalmente, já escolhe a data.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Aprovar e agendar são dois atos desde o ADR-018.</strong> Sem
/// <paramref name="ScheduleFor"/> este comando só aprova, e o boleto fica em <c>Approved</c> sem
/// data, esperando alguém agendar. Com ele, aprova e agenda <strong>na mesma transação</strong>
/// — é o "Aprovar e agendar" da tela, e ele é atômico de propósito: fossem duas chamadas HTTP,
/// a segunda falhando deixaria o boleto aprovado e sem data sem ninguém pedir isso.
/// </para>
/// </remarks>
/// <param name="AcknowledgeRisk">
/// ADR-015: obrigatório <c>true</c> para aprovar boleto classificado como Perigo ou Extremo
/// Perigo — é o aceite explícito que a trilha de auditoria grava.
/// </param>
/// <param name="RiskClearance">
/// A alçada de risco de quem aprova (nome de <c>RiskLevel</c>), resolvida pela BORDA a partir
/// dos escopos UMA — nunca vem do corpo da requisição, pelo mesmo motivo do UserId.
/// </param>
/// <param name="ActorName">
/// O nome de quem decide, para a trilha. Vem do token na BORDA, como o UserId — corpo de
/// requisição não escolhe em nome de quem a história é escrita.
/// </param>
/// <param name="AcknowledgeImmediateExecution">
/// ADR-017: obrigatório <c>true</c> para agendar boleto já vencido — o provedor o processa
/// imediatamente, sem agendamento, e pagar na hora exige aceite explícito gravado na trilha.
/// Só tem efeito quando <paramref name="ScheduleFor"/> vem preenchido.
/// </param>
public sealed record ApproveBillCommand(
    Guid TenantId,
    Guid BillId,
    Guid UserId,
    DateOnly? ScheduleFor,
    string? Note,
    string RiskClearance,
    string? ActorName = null,
    bool AcknowledgeRisk = false,
    bool AcknowledgeImmediateExecution = false) : ITenantScopedCommand, IRequest<ApproveBillResponse>;

/// <param name="ScheduledFor">Nulo quando a aprovação não veio acompanhada de agendamento.</param>
public sealed record ApproveBillResponse(Guid Id, string Status, DateOnly? ScheduledFor);

public sealed class ApproveBillCommandHandler(
    IBillRepository bills,
    IOptions<ApprovalOptions> options,
    IOptions<PaymentSchedulingOptions> schedulingOptions,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ApproveBillCommand, ApproveBillResponse>
{
    public async Task<ApproveBillResponse> Handle(ApproveBillCommand request, CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var bill = await bills.GetAsync(tenantId, BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var now = clock.GetUtcNow();

        // "Hoje" no fuso da POLÍTICA (America/Sao_Paulo), não em UTC: entre ~21h e meia-noite
        // locais o dia UTC já virou, e a guarda de vencido (BIL35) diria "vencido" de um boleto
        // que vence hoje — a tela (que vive no dia local) nem mostraria a caixa de aceite.
        var today = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeFromUtc(now.UtcDateTime, schedulingOptions.Value.ResolveTimeZone()));

        // Tradução de input: alçada desconhecida lança EnumerationNotFoundException → 400.
        var clearance = Enumeration.FromDisplayName<RiskLevel>(request.RiskClearance);

        var policy = options.Value.ToPolicy();
        var userId = UserId.From(request.UserId);

        // Todas as guardas — cobertura de checks, alçada de risco, aceite, validade do retrato e
        // teto de valor — vivem no método rico. O handler resolve política e data.
        bill.Approve(
            userId,
            request.Note,
            policy,
            clearance,
            now.UtcDateTime,
            request.AcknowledgeRisk,
            request.ActorName);

        // "Aprovar e agendar" na MESMA transação: dois métodos ricos, um save. Se o agendamento
        // for recusado (data inválida, vencido sem aceite), a aprovação cai junto — que é o
        // desfecho certo, porque foi isso que a pessoa pediu na tela, e não duas coisas soltas.
        if (request.ScheduleFor is { } scheduleFor)
        {
            bill.Schedule(
                userId,
                scheduleFor,
                policy,
                today,
                now.UtcDateTime,
                request.AcknowledgeImmediateExecution,
                request.ActorName);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ApproveBillResponse(bill.Id.Value, bill.Status.Name, bill.ScheduledFor);
    }
}

public sealed class ApproveBillIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ApproveBillIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ApproveBillCommand, ApproveBillResponse>(mediator, requestManager, logger)
{
    protected override ApproveBillResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty, default);
}
