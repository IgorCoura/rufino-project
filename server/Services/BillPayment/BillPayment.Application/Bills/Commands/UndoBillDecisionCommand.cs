namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Desfaz uma recusa ou um cancelamento: o boleto volta à fila de decisão e é revalidado
/// automaticamente (ADR-018).
/// </summary>
/// <remarks>
/// A revalidação não é chamada daqui — ela vem do <c>BillDecisionUndoneDomainEvent</c>, pelo
/// outbox, no mesmo molde da captura. Rodar as doze verificações dentro desta transação a
/// prenderia a duas consultas externas (boleto e Pix) e faria a reversão falhar quando o provedor
/// estivesse fora do ar, por um motivo que não é dela.
/// </remarks>
public sealed record UndoBillDecisionCommand(
    Guid TenantId,
    Guid BillId,
    Guid UserId,
    string Reason,
    string? ActorName = null) : ITenantScopedCommand, IRequest<UndoBillDecisionResponse>;

public sealed record UndoBillDecisionResponse(Guid Id, string Status);

public sealed class UndoBillDecisionCommandHandler(
    IBillRepository bills,
    IPaymentOrderRepository orders,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<UndoBillDecisionCommand, UndoBillDecisionResponse>
{
    public async Task<UndoBillDecisionResponse> Handle(
        UndoBillDecisionCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var billId = BillId.From(request.BillId);

        var bill = await bills.GetAsync(tenantId, billId, cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        // Duas pré-condições que o agregado não tem como conferir, porque ambas exigem consulta.
        // Elas só valem para quem de fato pode reverter: num boleto que ainda espera decisão, a
        // primeira encontra o PRÓPRIO boleto ocupando a chave natural e recusaria com
        // BLP.BIL02 — "já capturado", mandando procurar uma duplicata inexistente — em vez do
        // BLP.BIL38 que o agregado lança logo abaixo, e que é a recusa verdadeira.
        if (bill.AcceptsDecisionUndo)
        {
            // 1) A chave natural pode ter sido reocupada. Negar e cancelar LIBERAM a chave de
            //    deduplicação (BillStatus.OccupiesNaturalKey), então entre a decisão e a reversão o
            //    mesmo documento pode ter entrado de novo, legitimamente. Voltar sem conferir criaria
            //    dois boletos vivos para o mesmo compromisso — o índice único parcial em `bills` é o
            //    backstop da corrida, e este teste é o que dá a mensagem certa antes dele.
            if (bill.DedupKey is not null
                && await bills.ExistsActiveByDedupKeyAsync(bill.DedupKey, cancellationToken))
            {
                throw BillErrors.AlreadyCaptured();
            }

            // 2) Pode haver ordem viva no provedor. É o caso do boleto cancelado cuja ordem o
            //    provedor RECUSOU cancelar (hoje isso só vira LogWarning): o dinheiro ainda pode
            //    sair, e devolver o documento à fila de decisão convidaria a uma segunda aprovação
            //    para um pagamento que já está em curso.
            var activeOrder = await orders.GetActiveByBillAsync(tenantId, billId, cancellationToken);
            if (activeOrder is not null)
                throw BillErrors.UndoBlockedByLivePaymentOrder();
        }

        bill.UndoDecision(
            UserId.From(request.UserId),
            request.Reason,
            clock.GetUtcNow().UtcDateTime,
            request.ActorName);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new UndoBillDecisionResponse(bill.Id.Value, bill.Status.Name);
    }
}

public sealed class UndoBillDecisionIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<UndoBillDecisionIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<UndoBillDecisionCommand, UndoBillDecisionResponse>(
        mediator, requestManager, logger)
{
    protected override UndoBillDecisionResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty);
}
