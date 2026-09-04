namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// Uma pessoa confirma o pagamento imediato de uma ordem retida (boleto que venceu entre a
/// aprovação e a submissão — ADR-017). A autoria vem do token, nunca do corpo (ADR-007), e fica
/// gravada na ordem; a fila retoma sozinha na próxima janela.
/// </summary>
public sealed record ConfirmImmediatePaymentCommand(
    Guid TenantId,
    Guid PaymentOrderId,
    Guid ConfirmedBy) : ITenantScopedCommand, IRequest<ConfirmImmediatePaymentResponse>;

public sealed record ConfirmImmediatePaymentResponse(Guid PaymentOrderId, string Status);

public sealed class ConfirmImmediatePaymentCommandHandler(
    IPaymentOrderRepository orders,
    TimeProvider clock,
    IUnitOfWork unitOfWork)
    : IRequestHandler<ConfirmImmediatePaymentCommand, ConfirmImmediatePaymentResponse>
{
    public async Task<ConfirmImmediatePaymentResponse> Handle(
        ConfirmImmediatePaymentCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);

        var order = await orders.GetAsync(tenantId, PaymentOrderId.From(request.PaymentOrderId), cancellationToken)
            ?? throw PaymentOrderErrors.NotFound();

        order.ConfirmImmediateExecution(UserId.From(request.ConfirmedBy), clock.GetUtcNow().UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ConfirmImmediatePaymentResponse(order.Id.Value, order.Status.Name);
    }
}

public sealed class ConfirmImmediatePaymentIdentifiedCommandHandler(
    IMediator mediator,
    IRequestManager requestManager,
    ILogger<ConfirmImmediatePaymentIdentifiedCommandHandler> logger)
    : IdentifiedCommandHandler<ConfirmImmediatePaymentCommand, ConfirmImmediatePaymentResponse>(
        mediator, requestManager, logger)
{
    protected override ConfirmImmediatePaymentResponse CreateResultForDuplicateRequest()
        => new(Guid.Empty, string.Empty);
}
