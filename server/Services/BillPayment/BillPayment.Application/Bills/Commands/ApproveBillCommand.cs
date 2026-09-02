namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Bills.Checks;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>Um humano autoriza o pagamento e escolhe a data.</summary>
/// <param name="AcknowledgeRisk">
/// ADR-015: obrigatório <c>true</c> para aprovar boleto classificado como Perigo ou Extremo
/// Perigo — é o aceite explícito que a trilha de auditoria grava.
/// </param>
/// <param name="RiskClearance">
/// A alçada de risco de quem aprova (nome de <c>RiskLevel</c>), resolvida pela BORDA a partir
/// dos escopos UMA — nunca vem do corpo da requisição, pelo mesmo motivo do UserId.
/// </param>
/// <param name="AcknowledgeImmediateExecution">
/// ADR-017: obrigatório <c>true</c> para aprovar boleto já vencido — o provedor o processa
/// imediatamente, sem agendamento, e pagar na hora exige aceite explícito gravado na trilha.
/// </param>
public sealed record ApproveBillCommand(
    Guid TenantId,
    Guid BillId,
    Guid UserId,
    DateOnly ScheduleFor,
    string? Note,
    string RiskClearance,
    bool AcknowledgeRisk = false,
    bool AcknowledgeImmediateExecution = false) : ITenantScopedCommand, IRequest<ApproveBillResponse>;

public sealed record ApproveBillResponse(Guid Id, string Status, DateOnly ScheduledFor);

public sealed class ApproveBillCommandHandler(
    IBillRepository bills,
    IOptions<ApprovalOptions> options,
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

        // Tradução de input: alçada desconhecida lança EnumerationNotFoundException → 400.
        var clearance = Enumeration.FromDisplayName<RiskLevel>(request.RiskClearance);

        // Todas as guardas — cobertura de checks, alçada de risco, aceite, validade do retrato,
        // data e teto de valor — vivem no método rico. O handler resolve política e data.
        bill.Approve(
            UserId.From(request.UserId),
            request.ScheduleFor,
            request.Note,
            options.Value.ToPolicy(),
            clearance,
            DateOnly.FromDateTime(now.UtcDateTime),
            now.UtcDateTime,
            request.AcknowledgeRisk,
            request.AcknowledgeImmediateExecution);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ApproveBillResponse(bill.Id.Value, bill.Status.Name, bill.ScheduledFor!.Value);
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
