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

public sealed record ReconcilePaymentOrderResponse(Guid PaymentOrderId, string Outcome)
{
    /// <summary>O comando lançou. Não é desfecho dele — quem preenche é o laço da varredura.</summary>
    public const string OUTCOME_THREW = "Threw";

    /// <summary>
    /// A conciliação NÃO conseguiu fazer o trabalho dela — distinto de "conferiu e nada mudou".
    /// </summary>
    /// <remarks>
    /// Só estes três contam para o alerta de reincidência. <c>Unchanged</c> fica de fora de
    /// propósito: é o desfecho normal de uma ordem que segue legitimamente pendente no provedor,
    /// e contá-lo faria toda ordem em curso disparar alerta.
    /// </remarks>
    public bool IsBlocked
        => Outcome is "Unavailable" or "Incoherent" or OUTCOME_THREW;
}

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

    /// <summary>
    /// O provedor afirmou algo que não fecha — hoje, na prática: pago sem data de pagamento.
    /// </summary>
    /// <remarks>
    /// <strong>Existe porque a alternativa custou um pagamento.</strong> Até 2026-09-09 o
    /// <c>BLP.PMO03</c> subia daqui, o laço da varredura o registrava como falha genérica, e a
    /// MESMA ordem estourava a cada ciclo para sempre — a guarda do agregado lança antes de
    /// qualquer mutação, então nem a marca de sincronização ficava. Uma ordem paga de verdade no
    /// provedor ficou <c>Pending</c> por horas sem que nada dissesse por quê.
    /// </remarks>
    private const string OUTCOME_INCOHERENT = "Incoherent";

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

        var fetch = await order.GetFromProviderAsync(
            billGateway, pixGateway, credential, order.ProviderOrderId, cancellationToken);

        var nowUtc = clock.GetUtcNow();

        if (fetch.IsUnavailable)
        {
            // Este ramo foi MUDO até 2026-09-09, e o silêncio custou caro: com ele calado, uma
            // ordem podia ser reivindicada a cada ciclo sem uma linha de log dizendo por quê —
            // e nem `last_provider_sync_at` fica, porque não houve leitura para carimbar.
            logger.LogWarning(
                "Conciliação: não foi possível ler a ordem {PaymentOrderId} no provedor ({Reason}).",
                order.Id.Value, fetch.ReasonCode);

            return new ReconcilePaymentOrderResponse(request.PaymentOrderId, OUTCOME_UNAVAILABLE);
        }

        if (!fetch.IsFound)
        {
            // O provedor não conhece mais a ordem que ele mesmo aceitou — descompasso raro que
            // exige gente. Fica no log e a ordem continua na fila da conciliação, visível.
            logger.LogWarning(
                "Conciliação: o provedor não encontrou a ordem {PaymentOrderId}.", order.Id.Value);
            return new ReconcilePaymentOrderResponse(request.PaymentOrderId, OUTCOME_UNCHANGED);
        }

        var snapshot = fetch.Snapshot!;

        // Antes de traduzir: o que o provedor disse sobre si. Uma ordem que continua Pending
        // porque está esperando autorização de ação crítica não muda de status, e sem isto a
        // conciliação passaria por ela sem deixar rastro do motivo.
        order.RecordProviderDiagnostics(
            snapshot.RawStatus, snapshot.Authorized, snapshot.TransferId, nowUtc.UtcDateTime);

        bool applied;
        try
        {
            applied = order.ApplyProviderStatus(
                snapshot.Status, snapshot.PaidAt, snapshot.Fee, snapshot.FailReasons, nowUtc, nowUtc.UtcDateTime);
        }
        catch (DomainException ex) when (ex.Id == PaymentOrderErrors.INCOHERENT_PROVIDER_PAYLOAD_ID)
        {
            // ABSORVE, e grita. Deixar subir fazia a mesma ordem estourar a cada ciclo, para
            // sempre: a guarda do agregado lança antes de qualquer mutação, então nada era
            // gravado e nada mudava na próxima passagem. O log nomeia o status cru porque é ele
            // que diz o que o provedor afirmou e não conseguimos registrar.
            logger.LogError(
                ex,
                "Conciliação: o provedor descreve a ordem {PaymentOrderId} como {RawStatus}, mas o "
                + "retrato é incoerente. O dinheiro pode ter saído sem que o espelho registre — "
                + "verifique no painel do provedor.",
                order.Id.Value, snapshot.RawStatus);

            // Sem carimbo de sincronização de propósito: a ordem CONTINUA na fila da varredura,
            // visível, em vez de sumir por uma hora fingindo que foi conferida.
            return new ReconcilePaymentOrderResponse(request.PaymentOrderId, OUTCOME_INCOHERENT);
        }

        await unitOfWork.SaveEntitiesAsync(cancellationToken);

        return new ReconcilePaymentOrderResponse(
            request.PaymentOrderId, applied ? OUTCOME_APPLIED : OUTCOME_UNCHANGED);
    }
}
