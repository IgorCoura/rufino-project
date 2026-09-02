namespace BillPayment.Application.PaymentOrders.Commands;

using BillPayment.Application.Mediator;
using BillPayment.Domain.PaymentOrders;
using BillPayment.Domain.SeedWork;
using Microsoft.Extensions.Logging;

/// <summary>
/// Um evento <c>BILL_*</c> do provedor. O único Command do BC sem <c>TenantId</c>, e por
/// desenho: o Asaas não conhece nosso tenant — quem o resolve é a <c>externalReference</c>, que
/// É o id da ordem (UC-14).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Idempotente por id de evento</strong> (<c>payment_webhook_events</c>): a marca entra
/// no MESMO <c>SaveEntitiesAsync</c> do efeito, então reentrega não produz efeito dobrado, e
/// duas entregas concorrentes colidem na PK — a perdedora estoura, o provedor reentrega, e a
/// releitura encontra a marca.
/// </para>
/// <para>
/// Referência desconhecida é <c>Unknown</c>, nunca erro: devolver falha faria o provedor
/// retentar para sempre um evento que não é nosso — a mesma razão de o fora-de-ordem ser
/// ignorado pela <c>ApplyProviderStatus</c> monotônica.
/// </para>
/// </remarks>
public sealed record ProcessAsaasBillWebhookCommand(
    string EventId,
    string EventName,
    string? ExternalReference,
    DateOnly? PaidAt,
    decimal? Fee,
    IReadOnlyList<string> FailReasons) : IRequest<ProcessAsaasBillWebhookResponse>;

public sealed record ProcessAsaasBillWebhookResponse(string Outcome);

public sealed class ProcessAsaasBillWebhookCommandHandler(
    IPaymentOrderRepository orders,
    IPaymentWebhookLedger ledger,
    TimeProvider clock,
    IUnitOfWork unitOfWork,
    ILogger<ProcessAsaasBillWebhookCommandHandler> logger)
    : IRequestHandler<ProcessAsaasBillWebhookCommand, ProcessAsaasBillWebhookResponse>
{
    private const string OUTCOME_APPLIED = "Applied";
    private const string OUTCOME_IGNORED = "Ignored";
    private const string OUTCOME_DUPLICATE = "Duplicate";
    private const string OUTCOME_UNKNOWN = "Unknown";

    private const string BILL_EVENT_PREFIX = "BILL_";

    public async Task<ProcessAsaasBillWebhookResponse> Handle(
        ProcessAsaasBillWebhookCommand request,
        CancellationToken cancellationToken)
    {
        if (await ledger.ExistsAsync(request.EventId, cancellationToken))
            return new ProcessAsaasBillWebhookResponse(OUTCOME_DUPLICATE);

        var nowUtc = clock.GetUtcNow();
        await ledger.RecordAsync(request.EventId, nowUtc.UtcDateTime, cancellationToken);

        var order = string.IsNullOrWhiteSpace(request.ExternalReference)
            ? null
            : await orders.GetByExternalReferenceAsync(request.ExternalReference, cancellationToken);

        if (order is null)
        {
            // A marca persiste mesmo assim: o mesmo evento reentregue amanhã continua não sendo nosso.
            await unitOfWork.SaveEntitiesAsync(cancellationToken);
            logger.LogInformation("Webhook {EventName} sem ordem correspondente; ignorado.", request.EventName);
            return new ProcessAsaasBillWebhookResponse(OUTCOME_UNKNOWN);
        }

        var raw = request.EventName.StartsWith(BILL_EVENT_PREFIX, StringComparison.OrdinalIgnoreCase)
            ? request.EventName[BILL_EVENT_PREFIX.Length..]
            : request.EventName;

        var applied = order.ApplyProviderStatus(
            MapStatus(raw),
            request.PaidAt,
            request.Fee is { } fee ? new Domain.SharedKernel.Money(fee, Domain.SharedKernel.Currency.BRL) : null,
            request.FailReasons,
            nowUtc,
            nowUtc.UtcDateTime);

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ProcessAsaasBillWebhookResponse(applied ? OUTCOME_APPLIED : OUTCOME_IGNORED);
    }

    /// <summary>
    /// O mesmo mapa do adapter: desconhecido cai em <c>Pending</c>, que a monotônica ignora a
    /// partir de qualquer estado à frente — um evento novo do provedor nunca vira desfecho por chute.
    /// </summary>
    private static PaymentOrderStatus MapStatus(string raw)
        => raw.ToUpperInvariant() switch
        {
            "PAID" => PaymentOrderStatus.Paid,
            "BANK_PROCESSING" => PaymentOrderStatus.BankProcessing,
            "FAILED" => PaymentOrderStatus.Failed,
            "CANCELLED" => PaymentOrderStatus.Cancelled,
            "REFUNDED" => PaymentOrderStatus.Refunded,
            _ => PaymentOrderStatus.Pending,
        };
}
