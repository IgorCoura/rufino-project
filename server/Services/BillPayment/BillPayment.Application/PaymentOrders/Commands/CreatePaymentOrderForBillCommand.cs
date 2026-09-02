namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.Bills;
using BillPayment.Domain.PayerProfiles;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using BillPayment.Domain.SharedKernel;
using Microsoft.Extensions.Logging;

/// <summary>
/// A aprovação virou ordem — em <c>Draft</c>, <strong>sem nenhuma chamada externa</strong>.
/// Quem fala com o provedor é a fila de submissão, fora desta transação.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Idempotente sob a entrega at-least-once do outbox</strong> por dois cintos: a
/// consulta por ordem ativa do boleto, e o índice único parcial
/// <c>ix_payment_orders_bill_active</c> — uma corrida entre duas entregas morre no banco.
/// </para>
/// <para>
/// Tenant sem conta de pagamento não é erro: a ordem nasce retida em <c>AwaitingAccount</c>,
/// visível, e vincular a chave destrava pela própria fila (ADR-016). Boleto já vencido herda o
/// consentimento dado na aprovação (a guarda <c>BLP.BIL35</c> o exigiu lá) — sem ele a fila
/// pararia a ordem para perguntar de novo o que a pessoa acabou de responder.
/// </para>
/// </remarks>
public sealed record CreatePaymentOrderForBillCommand(
    Guid TenantId,
    Guid BillId,
    Guid ApprovedBy,
    DateOnly ScheduleFor) : ITenantScopedCommand, IRequest<CreatePaymentOrderForBillResponse>;

public sealed record CreatePaymentOrderForBillResponse(Guid PaymentOrderId, string Outcome);

public sealed class CreatePaymentOrderForBillCommandHandler(
    IBillRepository bills,
    IPaymentOrderRepository orders,
    IPayerProfileRepository payerProfiles,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<CreatePaymentOrderForBillCommandHandler> logger)
    : IRequestHandler<CreatePaymentOrderForBillCommand, CreatePaymentOrderForBillResponse>
{
    private const string OUTCOME_CREATED = "Created";
    private const string OUTCOME_ALREADY_EXISTS = "AlreadyExists";
    private const string OUTCOME_SKIPPED = "Skipped";

    public async Task<CreatePaymentOrderForBillResponse> Handle(
        CreatePaymentOrderForBillCommand request,
        CancellationToken cancellationToken)
    {
        var tenantId = TenantId.From(request.TenantId);
        var billId = BillId.From(request.BillId);

        var bill = await bills.GetAsync(tenantId, billId, cancellationToken)
            ?? throw BillErrors.NotFound(request.BillId);

        // A aprovação pode ter sido desfeita entre o evento e este handler (revalidação,
        // cancelamento). Criar ordem para um boleto que já não está aprovado pagaria algo que
        // ninguém autoriza mais — pular é o desfecho certo, não erro.
        if (bill.Status != BillStatus.Approved)
        {
            logger.LogInformation(
                "Boleto {BillId} não está mais em Approved ({Status}); nenhuma ordem criada.",
                request.BillId, bill.Status.Name);

            return new CreatePaymentOrderForBillResponse(Guid.Empty, OUTCOME_SKIPPED);
        }

        var existing = await orders.GetActiveByBillAsync(tenantId, billId, cancellationToken);
        if (existing is not null)
            return new CreatePaymentOrderForBillResponse(existing.Id.Value, OUTCOME_ALREADY_EXISTS);

        var now = clock.GetUtcNow().UtcDateTime;

        var order = PaymentOrder.Draft(
            tenantId,
            billId,
            bill.Rail,
            request.ScheduleFor,
            bill.AmountForPayment,
            now);

        var profile = await payerProfiles.GetByTenantAsync(tenantId, cancellationToken);
        if (profile is null || !profile.CanSchedulePayments)
            order.HoldForMissingAccount(now);

        if (bill.DueDate is { } due && due < DateOnly.FromDateTime(now))
            order.RecordImmediateExecutionConsent(UserId.From(request.ApprovedBy), now);

        await orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new CreatePaymentOrderForBillResponse(order.Id.Value, OUTCOME_CREATED);
    }
}
