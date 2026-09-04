namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;

/// <summary>
/// Reconfere uma ordem retida por falta de conta de pagamento e a devolve à fila quando o
/// tenant vinculou a chave — é o "vincular destrava pela própria fila" do ADR-016, sem acoplar
/// o vínculo da chave à existência de ordens.
/// </summary>
public sealed record ReleasePaymentOrderAccountHoldCommand(Guid TenantId, Guid PaymentOrderId)
    : ITenantScopedCommand, IRequest<ReleasePaymentOrderAccountHoldResponse>;

public sealed record ReleasePaymentOrderAccountHoldResponse(Guid PaymentOrderId, string Outcome);

public sealed class ReleasePaymentOrderAccountHoldCommandHandler(
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ReleasePaymentOrderAccountHoldCommand, ReleasePaymentOrderAccountHoldResponse>
{
    private const string OUTCOME_RELEASED = "Released";
    private const string OUTCOME_STILL_MISSING = "StillMissing";
    private const string OUTCOME_SKIPPED = "Skipped";

    public async Task<ReleasePaymentOrderAccountHoldResponse> Handle(
        ReleasePaymentOrderAccountHoldCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var order = await orders.GetAsync(tenantId, PaymentOrderId.From(request.PaymentOrderId), cancellationToken);
        if (order is null || order.Hold != PaymentOrderHold.AwaitingAccount)
            return new ReleasePaymentOrderAccountHoldResponse(request.PaymentOrderId, OUTCOME_SKIPPED);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        if (profile is null || !profile.CanSchedulePayments)
            return new ReleasePaymentOrderAccountHoldResponse(request.PaymentOrderId, OUTCOME_STILL_MISSING);

        order.ReleaseAccountHold(clock.GetUtcNow().UtcDateTime);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReleasePaymentOrderAccountHoldResponse(request.PaymentOrderId, OUTCOME_RELEASED);
    }
}
