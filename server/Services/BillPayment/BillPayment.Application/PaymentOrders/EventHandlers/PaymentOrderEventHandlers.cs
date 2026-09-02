namespace BillPayment.Application.PaymentOrders.EventHandlers;

using BillPayment.Application.Bills.Commands;
using BillPayment.Application.Mediator;
using BillPayment.Application.PaymentOrders.Commands;
using BillPayment.Domain.Bills;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

// Handlers do lado do pagamento (fase 3). Vivem na Application pelo motivo já registrado no
// BillCapturedDomainEventHandler: precisam do mediator, e Infra → Application seria ciclo.
// Todos idempotentes — o outbox entrega ao menos uma vez.

/// <summary>Aprovação → ordem em rascunho. O dinheiro só anda pela fila de submissão.</summary>
public sealed class CreatePaymentOrderOnBillApprovedHandler(IMediator mediator)
    : IDomainEventHandler<BillApprovedDomainEvent>
{
    public async Task HandleAsync(BillApprovedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new CreatePaymentOrderForBillCommand(
                domainEvent.TenantId.Value,
                domainEvent.BillId.Value,
                domainEvent.ApprovedBy.Value,
                domainEvent.ScheduleFor),
            cancellationToken);
    }
}

/// <summary>
/// Boleto cancelado leva a ordem junto — localmente quando ainda é rascunho, pelo provedor
/// quando já foi submetida e ainda dá.
/// </summary>
/// <remarks>
/// Falha do provedor sobe e o outbox retenta com backoff. Ordem que o provedor recusa cancelar
/// fica registrada em log e na fila operacional — o boleto já está cancelado, e o descompasso é
/// exatamente o que a conciliação e a fila de falhas existem para mostrar.
/// </remarks>
public sealed class CancelPaymentOrderOnBillCancelledHandler(
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    IBillPaymentGateway billGateway,
    IPixPaymentGateway pixGateway,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<CancelPaymentOrderOnBillCancelledHandler> logger)
    : IDomainEventHandler<BillCancelledDomainEvent>
{
    public async Task HandleAsync(BillCancelledDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        var order = await orders.GetActiveByBillAsync(
            domainEvent.TenantId, domainEvent.BillId, cancellationToken);

        if (order is null)
            return;

        var nowUtc = clock.GetUtcNow();
        var now = nowUtc.UtcDateTime;

        if (order.Status == PaymentOrderStatus.Draft)
        {
            order.CancelDraft(now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return;
        }

        if (!order.Status.AwaitsProviderOutcome || order.ProviderOrderId is null)
            return;

        var profile = await payerProfiles.GetByTenantAsync(domainEvent.TenantId, cancellationToken);
        var credential = profile?.AsaasAccountRef;

        var result = order.Rail == PaymentRail.Pix
            ? await pixGateway.CancelAsync(credential, order.ProviderOrderId, cancellationToken)
            : await billGateway.CancelAsync(credential, order.ProviderOrderId, cancellationToken);

        if (result.IsCancelled)
        {
            order.ApplyProviderStatus(
                PaymentOrderStatus.Cancelled, paidAt: null, fee: null, failReasons: null, nowUtc, now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return;
        }

        if (result.IsRetryable)
            throw PaymentOrderErrors.CancellationUnavailable(result.ReasonCode);

        // Recusa: a ordem já anda no provedor. O boleto está cancelado e a execução não — o
        // descompasso fica visível na conciliação e na fila operacional, nunca em silêncio.
        logger.LogWarning(
            "Boleto cancelado, mas o provedor recusou cancelar a ordem {PaymentOrderId} ({Reason}).",
            order.Id.Value, result.ReasonCode);
    }
}

/// <summary>Ordem aceita → boleto <c>Scheduled</c> (ADR-002).</summary>
public sealed class LinkBillOnPaymentOrderScheduledHandler(IMediator mediator)
    : IDomainEventHandler<PaymentOrderScheduledDomainEvent>
{
    public async Task HandleAsync(
        PaymentOrderScheduledDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new LinkBillToPaymentOrderCommand(
                domainEvent.TenantId.Value,
                domainEvent.BillId.Value,
                domainEvent.PaymentOrderId.Value,
                domainEvent.EffectiveScheduleDate),
            cancellationToken);
    }
}

/// <summary>Ordem paga → boleto <c>Paid</c>.</summary>
public sealed class ReflectPaymentPaidOnBillHandler(IMediator mediator)
    : IDomainEventHandler<PaymentOrderPaidDomainEvent>
{
    public async Task HandleAsync(
        PaymentOrderPaidDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new MarkBillPaidCommand(
                domainEvent.TenantId.Value, domainEvent.BillId.Value, domainEvent.PaymentOrderId.Value),
            cancellationToken);
    }
}

/// <summary>Ordem falhou → boleto <c>Failed</c> + aviso pelo canal do ADR-014.</summary>
public sealed class ReflectPaymentFailedOnBillHandler(
    IMediator mediator,
    INotificationSender notifications)
    : IDomainEventHandler<PaymentOrderFailedDomainEvent>
{
    public async Task HandleAsync(
        PaymentOrderFailedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new MarkBillPaymentFailedCommand(
                domainEvent.TenantId.Value, domainEvent.BillId.Value, domainEvent.PaymentOrderId.Value),
            cancellationToken);

        await notifications.SendAsync(
            domainEvent.TenantId,
            NotificationKind.PaymentFailed,
            new NotificationPayload(
                "Um pagamento agendado falhou",
                "O provedor não conseguiu concluir um pagamento agendado. Abra o boleto para ver o motivo e decidir a próxima tentativa.",
                $"/bill-payment/bills/{domainEvent.BillId.Value}"),
            cancellationToken);
    }
}

/// <summary>
/// Pago → busca e guarda o comprovante. O arquivo é a evidência; falha transiente sobe para a
/// reentrega do outbox retentar com backoff, e "sem comprovante" fica registrado, não escondido.
/// </summary>
public sealed class CaptureReceiptOnPaymentPaidHandler(IMediator mediator)
    : IDomainEventHandler<PaymentOrderPaidDomainEvent>
{
    public async Task HandleAsync(
        PaymentOrderPaidDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new CapturePaymentReceiptCommand(domainEvent.TenantId.Value, domainEvent.PaymentOrderId.Value),
            cancellationToken);
    }
}

/// <summary>Ordem cancelada depois de agendada → boleto <c>Cancelled</c>.</summary>
public sealed class ReflectPaymentCancelledOnBillHandler(IMediator mediator)
    : IDomainEventHandler<PaymentOrderCancelledDomainEvent>
{
    public async Task HandleAsync(
        PaymentOrderCancelledDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await mediator.Send(
            new MarkBillScheduleCancelledCommand(
                domainEvent.TenantId.Value, domainEvent.BillId.Value, domainEvent.PaymentOrderId.Value),
            cancellationToken);
    }
}

/// <summary>
/// Ordem retida em "aguardando confirmação" avisa o tenant — o boleto venceu entre a aprovação
/// e a submissão, e pagar na hora é decisão de gente (ADR-017).
/// </summary>
public sealed class NotifyPaymentAwaitingConfirmationHandler(INotificationSender notifications)
    : IDomainEventHandler<PaymentOrderHeldForConfirmationDomainEvent>
{
    public async Task HandleAsync(
        PaymentOrderHeldForConfirmationDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        await notifications.SendAsync(
            domainEvent.TenantId,
            NotificationKind.PaymentAwaitingConfirmation,
            new NotificationPayload(
                "Um pagamento aguarda a sua confirmação",
                "O boleto venceu antes de o agendamento ser feito, e conta vencida é paga imediatamente. Confirme se deseja pagar agora.",
                $"/bill-payment/bills/{domainEvent.BillId.Value}"),
            cancellationToken);
    }
}

/// <summary>Estorno depois de pago: fato raro e operacional — registra e avisa, gente decide.</summary>
public sealed class NotifyPaymentRefundedHandler(
    INotificationSender notifications,
    ILogger<NotifyPaymentRefundedHandler> logger)
    : IDomainEventHandler<PaymentOrderRefundedDomainEvent>
{
    public async Task HandleAsync(
        PaymentOrderRefundedDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        logger.LogWarning(
            "A ordem {PaymentOrderId} foi ESTORNADA pelo provedor depois de paga.",
            domainEvent.PaymentOrderId.Value);

        await notifications.SendAsync(
            domainEvent.TenantId,
            NotificationKind.PaymentFailed,
            new NotificationPayload(
                "Um pagamento foi estornado",
                "O provedor devolveu o valor de um pagamento já concluído. Verifique a conta e decida a próxima tentativa.",
                $"/bill-payment/bills/{domainEvent.BillId.Value}"),
            cancellationToken);
    }
}
