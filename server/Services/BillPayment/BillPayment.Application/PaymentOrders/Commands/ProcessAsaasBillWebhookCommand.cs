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
    private const string OUTCOME_INCOHERENT = "Incoherent";

    private const string BILL_EVENT_PREFIX = "BILL_";

    /// <summary><c>PaymentOrderErrors.IncoherentProviderPayload</c> — pago sem data de pagamento.</summary>
    private const string INCOHERENT_PAYLOAD_ERROR_ID = "BLP.PMO03";

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

            if (logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Webhook {EventName} sem ordem correspondente; ignorado.", request.EventName);

            return new ProcessAsaasBillWebhookResponse(OUTCOME_UNKNOWN);
        }

        var raw = request.EventName.StartsWith(BILL_EVENT_PREFIX, StringComparison.OrdinalIgnoreCase)
            ? request.EventName[BILL_EVENT_PREFIX.Length..]
            : request.EventName;

        var target = MapStatus(raw);

        bool applied;
        try
        {
            applied = order.ApplyProviderStatus(
                target,
                request.PaidAt,
                request.Fee is { } fee ? new Domain.SharedKernel.Money(fee, Domain.SharedKernel.Currency.BRL) : null,
                request.FailReasons,
                nowUtc,
                nowUtc.UtcDateTime);
        }
        catch (DomainException incoherent) when (
            string.Equals(incoherent.Id, INCOHERENT_PAYLOAD_ERROR_ID, StringComparison.Ordinal))
        {
            // Payload incoerente (pago sem data) responde 200 COM a marca do ledger persistida.
            // Deixar a exceção subir devolveria não-2xx SEM a marca — e o provedor reentregaria o
            // mesmo evento para sempre, represando a fila sequencial de webhooks da conta inteira.
            // Só o PMO03 é engolido: exceção de infraestrutura segue subindo, porque aí a
            // reentrega é exatamente o que se quer.
            await unitOfWork.SaveEntitiesAsync(cancellationToken);

            logger.LogError(
                incoherent,
                "Webhook {EventName} com payload INCOERENTE para a ordem {PaymentOrderId}. " +
                "Evento reconhecido e descartado; a conciliação segue vigiando a ordem.",
                request.EventName, order.Id.Value);

            return new ProcessAsaasBillWebhookResponse(OUTCOME_INCOHERENT);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        // Um "pago" chegando numa ordem que aqui já morreu é o pior descompasso possível —
        // dinheiro saiu no provedor com o espelho dizendo Cancelled/Failed. Nunca em silêncio.
        if (!applied && target == PaymentOrderStatus.Paid && order.Status.IsTerminal)
        {
            logger.LogWarning(
                "Webhook {EventName} diz PAGO, mas a ordem {PaymentOrderId} está terminal em {Status}. " +
                "Verifique o provedor: pode haver pagamento vivo para uma ordem encerrada.",
                request.EventName, order.Id.Value, order.Status.Name);
        }

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
