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
/// A rede de segurança do webhook: consulta o provedor por uma ordem parada e reflete o que ele
/// sabe. Webhook perdido não pode deixar ordem órfã (UC-15).
/// </summary>
public sealed record ReconcilePaymentOrderCommand(Guid TenantId, Guid PaymentOrderId)
    : ITenantScopedCommand, IRequest<ReconcilePaymentOrderResponse>;

public sealed record ReconcilePaymentOrderResponse(Guid PaymentOrderId, string Outcome);

public sealed class ReconcilePaymentOrderCommandHandler(
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    IBillPaymentGateway billGateway,
    IPixPaymentGateway pixGateway,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<ReconcilePaymentOrderCommandHandler> logger)
    : IRequestHandler<ReconcilePaymentOrderCommand, ReconcilePaymentOrderResponse>
{
    private const string OUTCOME_APPLIED = "Applied";
    private const string OUTCOME_UNCHANGED = "Unchanged";
    private const string OUTCOME_SKIPPED = "Skipped";
    private const string OUTCOME_UNAVAILABLE = "Unavailable";

    public async Task<ReconcilePaymentOrderResponse> Handle(
        ReconcilePaymentOrderCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var order = await orders.GetAsync(tenantId, PaymentOrderId.From(request.PaymentOrderId), cancellationToken);
        if (order is null || !order.Status.AwaitsProviderOutcome || order.ProviderOrderId is null)
            return new ReconcilePaymentOrderResponse(request.PaymentOrderId, OUTCOME_SKIPPED);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        var credential = profile?.AsaasAccountRef;

        var fetch = order.Rail == PaymentRail.Pix
            ? await pixGateway.GetAsync(credential, order.ProviderOrderId, cancellationToken)
            : await billGateway.GetAsync(credential, order.ProviderOrderId, cancellationToken);

        var nowUtc = clock.GetUtcNow();

        if (fetch.IsUnavailable)
            return new ReconcilePaymentOrderResponse(request.PaymentOrderId, OUTCOME_UNAVAILABLE);

        if (!fetch.IsFound)
        {
            // O provedor não conhece mais a ordem que ele mesmo aceitou — descompasso raro que
            // exige gente. Fica no log e a ordem continua na fila da conciliação, visível.
            logger.LogWarning(
                "Conciliação: o provedor não encontrou a ordem {PaymentOrderId}.", order.Id.Value);
            return new ReconcilePaymentOrderResponse(request.PaymentOrderId, OUTCOME_UNCHANGED);
        }

        var snapshot = fetch.Snapshot!;
        var applied = order.ApplyProviderStatus(
            snapshot.Status, snapshot.PaidAt, snapshot.Fee, snapshot.FailReasons, nowUtc, nowUtc.UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReconcilePaymentOrderResponse(
            request.PaymentOrderId, applied ? OUTCOME_APPLIED : OUTCOME_UNCHANGED);
    }
}
