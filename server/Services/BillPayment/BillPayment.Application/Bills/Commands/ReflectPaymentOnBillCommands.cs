namespace BillPayment.Application.Bills.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

// Os reflexos do ADR-002: do Scheduled em diante o Bill é ESPELHO da PaymentOrder, e estes são
// os únicos comandos que o movem — todos disparados por evento dela, nunca por caso de uso.
// Cada um é idempotente sob a entrega at-least-once do outbox: o estado-alvo já atingido é
// desfecho, não erro, e evento de uma ordem que não é mais a do boleto é ignorado (a ordem
// nova é quem manda).

/// <summary>O provedor aceitou a ordem: <c>Approved → Scheduled</c>, com a data efetiva.</summary>
public sealed record LinkBillToPaymentOrderCommand(
    Guid TenantId,
    Guid BillId,
    Guid PaymentOrderId,
    DateOnly EffectiveScheduleDate) : ITenantScopedCommand, IRequest<ReflectPaymentOnBillResponse>;

/// <summary>O dinheiro saiu: <c>Scheduled → Paid</c>.</summary>
public sealed record MarkBillPaidCommand(
    Guid TenantId,
    Guid BillId,
    Guid PaymentOrderId) : ITenantScopedCommand, IRequest<ReflectPaymentOnBillResponse>;

/// <summary>A execução não fechou: <c>Scheduled → Failed</c>. Os motivos vivem na ordem.</summary>
public sealed record MarkBillPaymentFailedCommand(
    Guid TenantId,
    Guid BillId,
    Guid PaymentOrderId) : ITenantScopedCommand, IRequest<ReflectPaymentOnBillResponse>;

/// <summary>A ordem foi cancelada depois de agendada: <c>Scheduled → Cancelled</c>.</summary>
public sealed record MarkBillScheduleCancelledCommand(
    Guid TenantId,
    Guid BillId,
    Guid PaymentOrderId) : ITenantScopedCommand, IRequest<ReflectPaymentOnBillResponse>;

public sealed record ReflectPaymentOnBillResponse(Guid BillId, string Status, bool Applied);

public sealed class LinkBillToPaymentOrderCommandHandler(
    IBillRepository bills,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<LinkBillToPaymentOrderCommand, ReflectPaymentOnBillResponse>
{
    public async Task<ReflectPaymentOnBillResponse> Handle(
        LinkBillToPaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        var bill = await bills.GetAsync(
                TenantId.From(request.TenantId), BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var orderId = PaymentOrderId.From(request.PaymentOrderId);

        if (bill.Status == BillStatus.Scheduled && bill.PaymentOrderId == orderId)
            return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: false);

        bill.LinkPaymentOrder(orderId, request.EffectiveScheduleDate, clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: true);
    }
}

public sealed class MarkBillPaidCommandHandler(
    IBillRepository bills,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkBillPaidCommand, ReflectPaymentOnBillResponse>
{
    public async Task<ReflectPaymentOnBillResponse> Handle(
        MarkBillPaidCommand request,
        CancellationToken cancellationToken)
    {
        var bill = await bills.GetAsync(
                TenantId.From(request.TenantId), BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        // Só o agendamento vivo DESTA ordem vira Paid: reentrega, ordem de rodada anterior e
        // boleto que já saiu de Scheduled (reaberto, cancelado) são ignorados — o estado que
        // vale é o do espelho, e forçar a transição por replay atrasado gravaria mentira.
        if (bill.Status != BillStatus.Scheduled
            || bill.PaymentOrderId != PaymentOrderId.From(request.PaymentOrderId))
        {
            return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: false);
        }

        bill.MarkPaid(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: true);
    }
}

public sealed class MarkBillPaymentFailedCommandHandler(
    IBillRepository bills,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkBillPaymentFailedCommand, ReflectPaymentOnBillResponse>
{
    public async Task<ReflectPaymentOnBillResponse> Handle(
        MarkBillPaymentFailedCommand request,
        CancellationToken cancellationToken)
    {
        var bill = await bills.GetAsync(
                TenantId.From(request.TenantId), BillId.From(request.BillId), cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var orderId = PaymentOrderId.From(request.PaymentOrderId);

        // Dois estados refletem a falha: Scheduled (o provedor aceitou e depois falhou — a
        // ordem é a vinculada) e Approved (a SUBMISSÃO foi recusada antes de agendar — o boleto
        // ainda nem tem vínculo, e o índice de ordem ativa única por boleto garante que o
        // evento é da rodada corrente). Qualquer outro estado é reentrega ou rodada antiga.
        var reflects =
            (bill.Status == BillStatus.Scheduled && bill.PaymentOrderId == orderId)
            || (bill.Status == BillStatus.Approved
                && (bill.PaymentOrderId is null || bill.PaymentOrderId == orderId));

        if (!reflects)
            return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: false);

        bill.MarkFailed(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: true);
    }
}

public sealed class MarkBillScheduleCancelledCommandHandler(
    IBillRepository bills,
    IPaymentOrderRepository orders,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<MarkBillScheduleCancelledCommand, ReflectPaymentOnBillResponse>
{
    public async Task<ReflectPaymentOnBillResponse> Handle(
        MarkBillScheduleCancelledCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var billId = BillId.From(request.BillId);

        var bill = await bills.GetAsync(tenantId, billId, cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        var orderId = PaymentOrderId.From(request.PaymentOrderId);
        var now = clock.GetUtcNow().UtcDateTime;

        // Agendamento vivo DESTA ordem: cancelamento pós-submissão espelha Scheduled → Cancelled.
        if (bill.Status == BillStatus.Scheduled && bill.PaymentOrderId == orderId)
        {
            bill.MarkScheduleCancelled(now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: true);
        }

        // Ordem cancelada AINDA EM RASCUNHO (o boleto nunca chegou a Scheduled, então não há
        // vínculo): o boleto aprovado volta à fila de decisão — a fila nunca mais criará ordem
        // para esta aprovação, e sem o reflexo ele ficaria Approved para sempre, sem saída
        // (ReopenForApproval só aceita Failed). A consulta de ordem ativa é a guarda contra a
        // reentrega tardia: existindo uma ordem viva (rodada NOVA de aprovação), o evento é
        // história e não desfaz nada.
        if (bill.Status == BillStatus.Approved && bill.PaymentOrderId is null)
        {
            var active = await orders.GetActiveByBillAsync(tenantId, billId, cancellationToken);
            if (active is not null && active.Id != orderId)
                return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: false);

            bill.ReturnToApprovalAfterScheduleCancellation(now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: true);
        }

        // Boleto cancelado por gente (a ordem morreu por consequência), reentrega ou rodada
        // anterior: nada a refletir.
        return new ReflectPaymentOnBillResponse(request.BillId, bill.Status.Name, Applied: false);
    }
}
