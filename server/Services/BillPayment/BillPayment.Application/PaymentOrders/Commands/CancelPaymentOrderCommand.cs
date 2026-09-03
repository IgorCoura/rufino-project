namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Instruments;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.Ports;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Cancela uma ordem — a janela de reação que a política das 24h existe para garantir.
/// </summary>
/// <remarks>
/// <c>Draft</c> cancela localmente (o provedor nem sabe dela). Depois da submissão o pedido vai
/// ao provedor primeiro, e o estado local só muda quando ele confirmar — cancelar aqui e falhar
/// lá deixaria a ordem "cancelada" pagando de verdade, a pior mentira possível. O reflexo no
/// <c>Bill</c> segue por evento, como toda mudança pós-agendamento (ADR-002).
/// </remarks>
public sealed record CancelPaymentOrderCommand(
    Guid TenantId,
    Guid PaymentOrderId,
    Guid RequestedBy) : ITenantScopedCommand, IRequest<CancelPaymentOrderResponse>;

public sealed record CancelPaymentOrderResponse(Guid PaymentOrderId, string Status);

public sealed class CancelPaymentOrderCommandHandler(
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    IBillPaymentGateway billGateway,
    IPixPaymentGateway pixGateway,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<CancelPaymentOrderCommandHandler> logger)
    : IRequestHandler<CancelPaymentOrderCommand, CancelPaymentOrderResponse>
{
    public async Task<CancelPaymentOrderResponse> Handle(
        CancelPaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var order = await orders.GetAsync(tenantId, PaymentOrderId.From(request.PaymentOrderId), cancellationToken)
            ?? throw PaymentOrderErrors.NotFound();

        var nowUtc = clock.GetUtcNow();
        var now = nowUtc.UtcDateTime;

        if (order.Status == PaymentOrderStatus.Draft)
        {
            order.CancelDraft(now);
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            return new CancelPaymentOrderResponse(order.Id.Value, order.Status.Name);
        }

        if (!order.Status.AwaitsProviderOutcome || order.ProviderOrderId is null)
            throw PaymentOrderErrors.CancellationNotAllowed(order.Status.Name);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        var credential = profile?.AsaasAccountRef;

        var result = order.Rail == PaymentRail.Pix
            ? await pixGateway.CancelAsync(credential, order.ProviderOrderId, cancellationToken)
            : await billGateway.CancelAsync(credential, order.ProviderOrderId, cancellationToken);

        if (!result.IsCancelled)
        {
            throw result.IsRetryable
                ? PaymentOrderErrors.CancellationUnavailable(result.ReasonCode)
                : PaymentOrderErrors.ProviderRefusedCancellation(result.ReasonCode);
        }

        order.ApplyProviderStatus(
            PaymentOrderStatus.Cancelled, paidAt: null, fee: null, failReasons: null, nowUtc, now);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "Ordem de pagamento {PaymentOrderId} cancelada a pedido de {RequestedBy}.",
                order.Id.Value, request.RequestedBy);
        }

        return new CancelPaymentOrderResponse(order.Id.Value, order.Status.Name);
    }
}

public sealed class CancelPaymentOrderIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<CancelPaymentOrderIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<CancelPaymentOrderCommand, CancelPaymentOrderResponse>(
        mediator, requestManager, logger)
{
    protected override CancelPaymentOrderResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty);
}
